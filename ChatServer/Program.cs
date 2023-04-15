using CommandManager;
using CLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using MessageHandler;

namespace ChatServer
{
    internal class Program
    {
        static Logger.Logger logger;
        static CommandManager.CommandManager commandManager;

        static ChatServer server;

        static Queue<Socket> validationQueue;
        static void Main(string[] args)
        {
            logger = new Logger.Logger();
            InputHandler.InputHandler inputHandler = new InputHandler.InputHandler();
            commandManager = new CommandManager.CommandManager();

            CLIPresenter presenter = new CLIPresenterImp(logger, inputHandler);
            CLI.CLI view = new CLI.CLI(presenter);

            inputHandler.InputProcessor += ProcessInput;
            logger.LogHistoryChanged += view.UpdateView;

            server = new ChatServer();

            bool isRunning = true;

            Command startCmd = new Command("start", cmdArgs =>
            {
                logger.LogInfo("Starting server...");

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000);
                server.Start(endPoint);

                if (server.IsListening())
                {
                    logger.LogSuccess("Server started succesfully!");

                    Thread connectionListenThread = new Thread(HandleNewConnectionsThread);
                    connectionListenThread.Start();

                    Thread clientConnectionThread = new Thread(HandleCurrentConnectionsThread);
                    clientConnectionThread.Start();
                }
                else
                {
                    logger.LogWarning("Unable to start server!");
                }
            });
            Command stopCmd = new Command("stop", cmdArgs =>
            {
                logger.LogInfo("Stopping server...");
                server.Stop();
                logger.LogSuccess("Server stopped succesfully!");
            });
            Command quitCmd = new Command("quit", cmdArgs =>
            {
                server.Stop();
                isRunning = false;
            });

            commandManager.AddCommand(startCmd);
            commandManager.AddCommand(stopCmd);
            commandManager.AddCommand(quitCmd);

            while (isRunning)
            {
                view.UpdateView();
                inputHandler.RegisterKeyPress();
            }
        }

        static void ProcessInput(string input)
        {
            if (input.StartsWith("/")) ProcessCommand(input.Substring(1));
            else logger.LogMessage(input);
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

        static void HandleNewConnectionsThread()
        {
            while (server.IsListening())
            {
                try
                {
                    if (server.HasPendingConnection())
                    {
                        Socket connection = server.AcceptConnection();
                        validationQueue.Enqueue(connection);
                    }
                }
                catch (ObjectDisposedException e)
                {
                    if (server.IsListening())
                    {
                        server.Stop();
                        break;
                    }
                }
            }
        }

        static void HandleCurrentConnectionsThread()
        {
            HandleValidations();
            Queue<Message> messages = server.CollectMessagesFromConnectedClients();
            HandlePendingMessages(messages);
        }

        static void HandleValidations()
        {
            while (validationQueue.Count > 0)
            {
                Socket connectionToValidate = validationQueue.Dequeue();

                logger.LogInfo("Connecting client...");
                var (username, password) = GetConnectionLoginInfo(connectionToValidate);

                bool isConnectionValid = false;

                if (!string.IsNullOrEmpty(username))
                {
                    if (server.IsClientConnected(username))
                    {
                        logger.LogInfo($"Client with username '{username}' is already connected!");

                    }
                    else
                    {
                        isConnectionValid = true;
                    }
                }

                if (isConnectionValid)
                {
                    logger.LogSuccess("Succesfully connected new client!");
                    server.AddClientConnection(username, connectionToValidate);
                }
                else
                {
                    logger.LogInfo("Client connection rejected.");
                    connectionToValidate.Close(5);
                }
            }
        }

        static (string username, string password) GetConnectionLoginInfo(Socket connection)
        {
            string username = null;
            string password = null;

            connection.ReceiveTimeout = 5 * 1000;
            connection.SendTimeout = 5 * 1000;

            try
            {
                Message loginInfoRequest = Message.Request("Provide username and password");
                loginInfoRequest.AddArgument("request", "LoginInfo");

                MessagingHandler.SendMessage(loginInfoRequest, connection);

                Message connectionResponse = MessagingHandler.ReceiveMessage(connection);

                if(connectionResponse.messageHeader == MessageHeader.Response)
                {
                    if(connectionResponse.HasArgument("username"))
                        username = connectionResponse.GetArgumentValue("username");

                    if (connectionResponse.HasArgument("password"))
                        password = connectionResponse.GetArgumentValue("password");
                }
            }
            catch (SocketException e)
            {
                logger.LogInfo("Connection timed out!");
                return (null, null);
            }

            connection.ReceiveTimeout = 0;
            connection.SendTimeout = 0;

            return (username, password);
        }

        static void HandlePendingMessages(Queue<Message> pendingMessages)
        {
            while (pendingMessages.Count > 0)
            {
                Message pendingMessage = pendingMessages.Dequeue();

                switch (pendingMessage.messageHeader)
                {
                    case MessageHeader.Request:
                        HandleRequest(pendingMessage);
                        break;
                    case MessageHeader.Public_Message:
                        server.BroadcastMessage(pendingMessage);
                        break;
                    case MessageHeader.Private_Message:
                        server.SendPrivateMessage(pendingMessage);
                        break;
                }
            }
        }

        static void HandleRequest(Message request)
        {
            //if (request.messageArg.Equals("disconnect"))
            //{
            //    server.DisconnectClientIfExist(request.messageBody);
            //}
        }
    }
}
