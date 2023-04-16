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

            validationQueue = new Queue<Socket>();

            server = new ChatServer();

            bool isRunning = true;

            Command startCmd = new Command("start", cmdArgs =>
            {
                if (server.IsListening)
                {
                    logger.LogWarning("Server is already started!");
                    return;
                }

                logger.LogInfo("Starting server...");

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000);

                try
                {
                    server.Start(endPoint);
                }
                catch(SocketException e)
                {
                    logger.LogWarning("Unable to start server!");
                    logger.LogError(e.Message);
                }


                if (server.IsListening)
                {
                    logger.LogSuccess("Server started succesfully!");

                    Thread connectionListenThread = new Thread(HandleNewConnectionsThread);
                    connectionListenThread.Start();

                    Thread clientConnectionThread = new Thread(HandleCurrentConnectionsThread);
                    clientConnectionThread.Start();
                }
            });
            Command stopCmd = new Command("stop", cmdArgs =>
            {
                logger.LogInfo("Stopping server...");

                Message serverClosedStatus = Message.ConnectionStatus("Server closed!");
                serverClosedStatus.AddArgument("status", "disconnected");
                server.BroadcastMessage(serverClosedStatus);

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

            commandManager.ExecuteCommand("start");

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
            while (server.IsListening)
            {
                try
                {
                    if (server.HasPendingConnection())
                    {
                        Socket connection = server.AcceptConnection();
                        if(connection != null) validationQueue.Enqueue(connection);
                    }
                }
                catch (Exception e) when (e is ObjectDisposedException || e is NullReferenceException)
                {
                    if (server.IsListening)
                    {
                        server.Stop();
                        break;
                    }
                }
                catch(SocketException e)
                {
                    server.Stop();
                    logger.LogError(e.Message);
                    break;
                }
            }
        }

        static void HandleCurrentConnectionsThread()
        {
            while (server.IsListening)
            {
                HandleValidations();
                Queue<Message> messages = server.CollectMessagesFromClients();
                HandlePendingMessages(messages);
                DisconnectClientsWhoLostConnection();
            }
        }

        static void HandleValidations()
        {
            while(validationQueue.Count > 0)
            {
                Socket connectionToValidate = validationQueue.Dequeue();
                IPEndPoint endPoint = (IPEndPoint)connectionToValidate.RemoteEndPoint;

                logger.LogInfo($"Connecting client at {endPoint.Address}:{endPoint.Port}...");
                var (username, password) = GetConnectionLoginInfo(connectionToValidate);

                if (username is null)
                {
                    connectionToValidate.Close(5);
                    return;
                }

                bool isUsernameValid = ValidateUsername(username);

                if (isUsernameValid) AcceptNewConnectionWithUsername(connectionToValidate, username);
                else RejectConnection(connectionToValidate, "Invalid Username!");
            }
        }

        static (string username, string password) GetConnectionLoginInfo(Socket connection)
        {
            string username = null;
            string password = null;
            try
            {
                connection.ReceiveTimeout = 5 * 1000;
                connection.SendTimeout = 5 * 1000;

                Message loginInfoRequest = Message.Request("Provide username and password");
                loginInfoRequest.AddArgument("request", "LoginInfo");

                MessagingHandler.SendMessage(loginInfoRequest, connection);

                Message connectionResponse = MessagingHandler.ReceiveMessage(connection);

                if (connectionResponse.messageHeader == MessageHeader.Response)
                {
                    if (connectionResponse.HasArgument("username"))
                        username = connectionResponse.GetArgumentValue("username");

                    if (connectionResponse.HasArgument("password"))
                        password = connectionResponse.GetArgumentValue("password");
                }
            }
            catch (SocketException)
            {
                logger.LogInfo("Connection timed out!");
                return (null, null);
            }

            return (username, password);
        }

        static bool ValidateUsername(string username)
        {
            return !string.IsNullOrEmpty(username) && !server.IsClientConnectedWithUsername(username);
        }

        static void AcceptNewConnectionWithUsername(Socket connection, string username)
        {
            try
            {
                Message status = Message.ConnectionStatus("Succesfully connected to Server!");
                status.AddArgument("status", "Connected");
                MessagingHandler.SendMessage(status, connection);
            }
            catch(SocketException)
            {
                connection.Close(5);
                logger.LogInfo("Connection lost.");
            }

            logger.LogSuccess($"Succesfully connected new client with username: {username}");
            server.AddClientConnection(username, new Client(connection, username));
        }

        static void RejectConnection(Socket connection, string reason)
        {
            logger.LogInfo($"Client connection rejected: {reason}");

            try
            {
                Message status = Message.ConnectionStatus($"Connection rejected: {reason}");
                status.AddArgument("status", "Rejected");
                MessagingHandler.SendMessage(status, connection);
            }
            catch (SocketException) { }
            finally
            {
                connection.Close(5);
            }
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
                        logger.LogMessage($"<{pendingMessage.GetArgumentValue("sender")}> {pendingMessage.messageBody}");
                        server.BroadcastMessage(pendingMessage);
                        break;
                    case MessageHeader.Private_Message:
                        logger.LogInfo($"PRIVATE: <{pendingMessage.GetArgumentValue("sender")}> -> <{pendingMessage.GetArgumentValue("receiver")}> {pendingMessage.messageBody}");
                        server.SendMessage(pendingMessage);
                        break;
                }
            }
        }

        static void HandleRequest(Message request)
        {
            if (request.HasArgument("request"))
            {
                string requestedAction = request.GetArgumentValue("request");

                if(requestedAction.Equals("disconnect", StringComparison.OrdinalIgnoreCase))
                {
                    string username = request.GetArgumentValue("sender");

                    Message disconnectStatus = Message.ConnectionStatus("Succesfully disconnected!");
                    disconnectStatus.AddArgument("status", "disconnected");
                    disconnectStatus.AddArgument("receiver", username);
                    server.SendMessage(disconnectStatus);

                    server.DisconnectClient(username);

                    logger.LogInfo($"Client {username} disconnected: Disconnect requested");

                    Message disconnectInfo = Message.Server($"Client '{username}' disconnected!");

                    server.BroadcastMessage(disconnectInfo);
                }
                else if (requestedAction.Equals("UserList", StringComparison.OrdinalIgnoreCase))
                {
                    string username = request.GetArgumentValue("sender");

                    Message userList = Message.Response(String.Join("\n", server.GetClientsUsernames()));
                    userList.AddArgument("requested", "UserList");
                    userList.AddArgument("receiver", username);

                    server.SendMessage(userList);
                }
            }
        }

        static void DisconnectClientsWhoLostConnection()
        {
            List<Client> clients = server.GetClientsWhoLostConnection();

            foreach (Client client in clients)
            {
                logger.LogInfo($"Client '{client.Username}' lost connection!");

                Message disconnectInfo = Message.Server($"Client: {client.Username} lost connection!");

                try
                {
                    server.BroadcastMessage(disconnectInfo);
                }
                catch (SocketException) { }

                server.DisconnectClient(client.Username);
            }
        }
    }
}
