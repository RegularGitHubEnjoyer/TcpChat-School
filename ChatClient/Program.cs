using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections;
using Utility.CommandInterface;
using Utility.Commands;
using Utility.Input;
using Utility.Logging;
using Utility.Messaging;

namespace ChatClient
{
    internal class Program
    {
        static Logger logger;
        static CommandManager commandManager;

        static ChatClient client;

        static List<Message> messagesToSend;
        static string username;

        static void Main(string[] args)
        {
            logger = new Logger();
            InputHandler inputHandler = new InputHandler();
            commandManager = new CommandManager();

            CLIPresenter presenter = new CLIPresenterImp(logger, inputHandler);
            CLI view = new CLI(presenter);

            inputHandler.InputProcessor += ProcessInput;
            logger.LogHistoryChanged += view.UpdateView;

            messagesToSend = new List<Message>();
            username = "";

            client = new ChatClient();

            bool isRunning = true;

            Command connectCmd = new Command("connect", cmdArgs =>
            {
                if (client.IsConnected)
                {
                    logger.LogWarning("Already connected!");
                    return;
                }

                if(cmdArgs.Count == 1)
                {
                    logger.LogInfo("Connecting to server...");
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 11000);

                    username = cmdArgs[0];

                    try
                    {
                        client.Connect(endPoint);
                    }
                    catch (SocketException e)
                    {
                        logger.LogWarning("Unable to connect to a server!");
                        logger.LogError(e.Message);
                    }

                    if (client.IsConnected)
                    {
                        Thread connectionThread = new Thread(HandleConnectionThread);
                        connectionThread.Start();
                    }
                }
                else
                {
                    logger.LogWarning("Incorrect use of command 'connect'");
                }
            });
            connectCmd.SetCommandDescription("Connects to the server if it's not already connected.");
            connectCmd.SetCommandHelp("Connects to the server if it's not already connected.\n/connect [username]\t/connect myUsername");

            Command disconnectCmd = new Command("disconnect", cmdArgs =>
            {
                client.RequestDisconnect();
            });
            disconnectCmd.SetCommandDescription("Requests disconnect from the server if it's connected.");
            disconnectCmd.SetCommandHelp("Requests disconnect from the server if it's connected.");

            Command quitCmd = new Command("quit", cmdArgs =>
            {
                if (client.IsConnected)
                    client.RequestDisconnect();
                isRunning = false;
            });
            quitCmd.SetCommandDescription("Requests disconnect from the server if it's connected and closes app.");
            quitCmd.SetCommandHelp("Requests disconnect from the server if it's connected and closes app.");

            Command userlistCmd = new Command("userlist", cmdArgs =>
            {
                if (client.IsConnected)
                {
                    Message userListRequest = Message.Request("");
                    userListRequest.AddArgument("request", "userlist");

                    messagesToSend.Add(userListRequest);
                }
                else
                {
                    logger.LogWarning("You have to be connected to request user list!");
                }
            });
            userlistCmd.SetCommandDescription("Requests list of connected users and displays them.");
            userlistCmd.SetCommandHelp("Requests list of connected users and displays them.");

            Command pmCmd = new Command("pm", cmdArgs =>
            {
                if (client.IsConnected)
                {
                    if (cmdArgs.Count > 1)
                    {
                        Message pm = Message.PrivateMessage(String.Join(" ", cmdArgs.Skip(1)));
                        pm.AddArgument("receiver", cmdArgs[0]);

                        messagesToSend.Add(pm);
                    }
                    else
                    {
                        logger.LogWarning("Invalid use of 'pm' command!");
                    }
                }
                else
                {
                    logger.LogWarning("You have to be connected to send private messages!");
                }
            });
            pmCmd.SetCommandDescription("Sends private message to specified user.");
            pmCmd.SetCommandHelp("Sends private message to specified user.\n/pm [username] [message]\t/pm user1 example of private message.");

            Command helpCmd = new Command("help", cmdArgs =>
            {
                if (cmdArgs.Count == 0)
                {
                    List<(string cmd, string description)> availableCommands = commandManager.GetAvaliableCommandsWithDescription();
                    int longestCommandName = availableCommands.Max(cmd => cmd.cmd.Length);
                    int numberingPadding = (availableCommands.Count.ToString()).Length;

                    logger.LogMessage("Available commands:");
                    for (int i = 0; i < availableCommands.Count; i++)
                    {
                        logger.LogMessage($"{(i + 1).ToString().PadRight(numberingPadding)} {availableCommands[i].cmd.PadRight(longestCommandName, ' ')}\t{availableCommands[i].description}");
                    }
                }
                else if (cmdArgs.Count == 1)
                {
                    if (commandManager.CommandAvailable(cmdArgs[0]))
                    {
                        logger.LogInfo($"Help page for '{cmdArgs[0]}':");
                        foreach(string line in commandManager.GetCommandHelp(cmdArgs[0]).Split('\n'))
                            logger.LogMessage(line);
                    }
                    else
                    {
                        logger.LogWarning($"Unknown command '{cmdArgs[0]}'");
                    }
                }
                else
                {
                    logger.LogWarning($"'help' takes max 1 argument, provided: {cmdArgs.Count}");
                }
            });
            helpCmd.SetCommandDescription("display list of available commands and help pages.");
            helpCmd.SetCommandHelp("/help\tdisplay list of available commands.\n/help [command]\tdisplay [command] help page");


            commandManager.AddCommand(connectCmd);
            commandManager.AddCommand(disconnectCmd);
            commandManager.AddCommand(quitCmd);
            commandManager.AddCommand(userlistCmd);
            commandManager.AddCommand(pmCmd);
            commandManager.AddCommand(helpCmd);

            logger.LogInfo("Type '/help' to display list of available commands.");
            logger.LogInfo("Type '/help [command]' to display help page for specified command.");

            while (isRunning)
            {
                view.UpdateView();
                inputHandler.RegisterKeyPress();
            }
        }

        static void ProcessInput(string input)
        {
            if (input.StartsWith("/"))
            {
                ProcessCommand(input.Substring(1));
            }
            else if(!string.IsNullOrEmpty(input))
            {
                if (client.IsConnected) messagesToSend.Add(Message.PublicMessage(input));
                logger.LogMessage(input);
            }
        }

        static void ProcessCommand(string commandString)
        {
            string[] commandParts = commandString.Split(' ');
            if (commandManager.CommandAvailable(commandParts[0]))
            {
                CommandArgs args = new CommandArgs(commandParts.Skip(1).ToArray());
                commandManager.ExecuteCommand(commandParts[0], args);
            }
            else
            {
                logger.LogWarning($"Unknown command: '{commandParts[0]}'");
            }
        }

        static void HandleConnectionThread()
        {
            while (client.IsConnected)
            {
                Queue<Message> messages = GetPendingMessages();
                HandleReceivedMessages(messages);
                HandleSendingMessages();
                if (client.LostConnectionWithServer())
                {
                    logger.LogInfo("Lost connection with server");
                    client.Disconnect();
                }
            }
        }

        static Queue<Message> GetPendingMessages()
        {
            try
            {
                return client.GetPendingMessages();
            }
            catch (Exception e) when (e is ObjectDisposedException || e is NullReferenceException) { }
            catch (SocketException)
            {
                client.Disconnect();
                logger.LogInfo("Connection with servert timed out!");
            }

            return new Queue<Message>();
        }

        static void HandleReceivedMessages(Queue<Message> messages)
        {
            if (!client.IsConnected) return;

            HandleConnectionStatusMessages(messages.Where(msg => msg.messageHeader == MessageHeader.Connection_Status));

            HandleRequests(messages.Where(msg => msg.messageHeader == MessageHeader.Request));

            HandleResponses(messages.Where(msg => msg.messageHeader == MessageHeader.Response));

            HandleServerMessages(messages.Where(msg => msg.messageHeader == MessageHeader.Server));

            HandleChatMessages(messages.Where(msg => msg.messageHeader == MessageHeader.Public_Message || msg.messageHeader == MessageHeader.Private_Message));
        }

        static void HandleConnectionStatusMessages(IEnumerable<Message> messages)
        {
            foreach (Message msg in messages)
            {
                if (msg.HasArgument("status"))
                {
                    string status = msg.GetArgumentValue("status");

                    if (status.Equals("connected", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogSuccess(msg.messageBody);
                    }
                    else if (status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        client.Disconnect();
                        logger.LogWarning(msg.messageBody);
                    }
                    else if (status.Equals("disconnected", StringComparison.OrdinalIgnoreCase))
                    {
                        client.Disconnect();
                        logger.LogInfo(msg.messageBody);
                    }
                }
            }
        }

        static void HandleRequests(IEnumerable<Message> messages)
        {
            foreach (Message msg in messages)
            {
                if (msg.HasArgument("request"))
                {
                    if (msg.GetArgumentValue("request").Equals("LoginInfo", StringComparison.OrdinalIgnoreCase))
                    {
                        Message response = Message.Response("Providing login info!");
                        response.AddArgument("username", username);

                        messagesToSend.Add(response);
                    }
                }
            }
        }

        static void HandleResponses(IEnumerable<Message> messages)
        {
            foreach (Message msg in messages)
            {
                if (msg.HasArgument("requested"))
                {
                    if (msg.GetArgumentValue("requested").Equals("UserList", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] usernames = msg.messageBody.Split(',');
                        int numberingPadding = (usernames.Length.ToString()).Length;

                        logger.LogMessage("Connected users:");
                        for (int i = 0; i < usernames.Length; i++)
                        {
                            logger.LogMessage($"{(i + 1).ToString().PadRight(numberingPadding)} {usernames[i]}");
                        }
                    }
                }
            }
        }

        static void HandleServerMessages(IEnumerable<Message> messages)
        {
            foreach (Message msg in messages)
            {
                logger.LogInfo($"SERVER: {msg.messageBody}");
            }
        }

        static void HandleChatMessages(IEnumerable<Message> messages)
        {
            foreach (Message msg in messages)
            {
                if (msg.messageHeader == MessageHeader.Public_Message)
                {
                    if (msg.HasArgument("sender"))
                    {
                        logger.LogMessage($"<{msg.GetArgumentValue("sender")}> {msg.messageBody}");
                    }
                    else
                    {
                        logger.LogMessage($"?ANONYMOUS? {msg.messageBody}");
                    }
                }
                else if(msg.messageHeader == MessageHeader.Private_Message)
                {
                    if (msg.HasArgument("sender"))
                    {
                        logger.LogInfo($"Private: <{msg.GetArgumentValue("sender")}> {msg.messageBody}");
                    }
                    else
                    {
                        logger.LogInfo($"Private: ?ANONYMOUS? {msg.messageBody}");
                    }
                }
            }
        }

        static void HandleSendingMessages()
        {
            if (!client.IsConnected) return;

            try
            {
                SendMessages(messagesToSend.Where(msg => msg.messageHeader == MessageHeader.Response));
                SendMessages(messagesToSend.Where(msg => msg.messageHeader == MessageHeader.Request));
                SendMessages(messagesToSend.Where(msg => msg.messageHeader == MessageHeader.Public_Message || msg.messageHeader == MessageHeader.Private_Message));
            }
            catch (Exception e) when (e is ObjectDisposedException || e is NullReferenceException) { }
            catch (SocketException)
            {
                client.Disconnect();
                logger.LogInfo("Connection with servert timed out!");
            }
            finally
            {
                messagesToSend.Clear();
            }
        }

        static void SendMessages(IEnumerable<Message> messages)
        {
            foreach (Message message in messages)
            {
                client.SendMessage(message);
            }
        }
    }
}
