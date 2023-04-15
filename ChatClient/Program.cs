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

namespace ChatClient
{
    internal class Program
    {
        static Logger.Logger logger;
        static CommandManager.CommandManager commandManager;

        static ChatClient client;

        static Queue<Message> messagesToSend;

        static void Main(string[] args)
        {
            logger = new Logger.Logger();
            InputHandler.InputHandler inputHandler = new InputHandler.InputHandler();
            commandManager = new CommandManager.CommandManager();

            CLIPresenter presenter = new CLIPresenterImp(logger, inputHandler);
            CLI.CLI view = new CLI.CLI(presenter);

            inputHandler.InputProcessor += ProcessInput;
            logger.LogHistoryChanged += view.UpdateView;

            messagesToSend = new Queue<Message>();

            client = new ChatClient();

            bool isRunning = true;

            Command connectCmd = new Command("connect", cmdArgs =>
            {
                logger.LogInfo("Connecting to server...");
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 11000);

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
                    logger.LogSuccess("Succesfully connected to a server!");

                    Thread connectionThread = new Thread(HandleConnectionThread);
                    connectionThread.Start();
                }
            });
            Command quitCmd = new Command("quit", cmdArgs =>
            {
                if (client.IsConnected)
                    client.RequestDisconnect();
                isRunning = false;
            });


            commandManager.AddCommand(quitCmd);

            while (isRunning)
            {
                view.UpdateView();
                inputHandler.RegisterKeyPress();
            }
        }

        static void ProcessInput(string input)
        {
            if (input.StartsWith("/"))
                ProcessCommand(input.Substring(1));
            else if (client.IsConnected)
                if (!string.IsNullOrEmpty(input)) client.SendChatMessage(input);
                else
                    logger.LogMessage(input);
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
                Queue<Message> messages = client.GetPendingMessages();
                HandleReceivedMessages(messages);
                SendMessages();
            }
        }

        static void HandleReceivedMessages(Queue<Message> messages)
        {
            var groupedMessages = messages.GroupBy(msg => msg.messageHeader);

            foreach(Message msg in groupedMessages.Where(group => group.Key == MessageHeader.Connection_Status))
            {
                if(msg.HasArgument("status"))
                {
                    if(msg.GetArgumentValue("status").Equals("disconnected", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInfo(msg.messageBody);
                        client.Disconnect();
                        return;
                    }
                    else if (msg.GetArgumentValue("status").Equals("connected", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogSuccess(msg.messageBody);
                    }
                }
            }

            foreach (Message msg in groupedMessages.Where(group => group.Key == MessageHeader.Request))
            {
                if (msg.HasArgument("request"))
                {
                    if (msg.GetArgumentValue("request").Equals("LoginInfo", StringComparison.OrdinalIgnoreCase))
                    {
                        Message response = Message.Response("Providing login info");
                        response.AddArgument("username", "Guest");
                    }
                }
            }

            foreach (Message msg in groupedMessages.Where(group => group.Key == MessageHeader.Response))
            {

            }

            foreach (Message msg in groupedMessages.Where(group => group.Key == MessageHeader.Public_Message || group.Key == MessageHeader.Private_Message))
            {
                if(msg.messageHeader == MessageHeader.Private_Message)
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
                else
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
            }
        }

        static void SendMessages()
        {
            while(messagesToSend.Count > 0)
            {
                client.SendMessage(messagesToSend.Dequeue());
            }
        }
    }
}
