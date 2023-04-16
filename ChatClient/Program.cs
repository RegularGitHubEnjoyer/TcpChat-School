using CommandManager;
using CLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using MessageHandler;
using System.Collections;
using System.Runtime.Remoting.Messaging;

namespace ChatClient
{
    internal class Program
    {
        static Logger.Logger logger;
        static CommandManager.CommandManager commandManager;

        static ChatClient client;

        static List<Message> messagesToSend;
        static string username;

        static void Main(string[] args)
        {
            logger = new Logger.Logger();
            InputHandler.InputHandler inputHandler = new InputHandler.InputHandler();
            commandManager = new CommandManager.CommandManager();

            CLIPresenter presenter = new CLIPresenterImp(logger, inputHandler);
            CLI.CLI view = new CLI.CLI(presenter);

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

                logger.LogInfo("Connecting to server...");
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 11000);

                if(cmdArgs.Count == 1)
                {
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
            Command disconnectCmd = new Command("disconnect", cmdArgs =>
            {
                client.RequestDisconnect();
            });
            Command quitCmd = new Command("quit", cmdArgs =>
            {
                if (client.IsConnected)
                    client.RequestDisconnect();
                isRunning = false;
            });
            Command userlistCmd = new Command("userlist", cmdArgs =>
            {
                Message userListRequest = Message.Request("");
                userListRequest.AddArgument("request", "userlist");

                messagesToSend.Add(userListRequest);
            });


            commandManager.AddCommand(connectCmd);
            commandManager.AddCommand(disconnectCmd);
            commandManager.AddCommand(quitCmd);
            commandManager.AddCommand(userlistCmd);

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
                        logger.LogInfo(msg.messageBody);
                    }
                }
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
