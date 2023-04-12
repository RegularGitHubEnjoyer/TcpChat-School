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

                    Thread connectionListenThread = new Thread(HandleNewConnections);
                    connectionListenThread.Start();

                    Thread clientConnectionThread = new Thread(HandleCurrentConnections);
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

        static void HandleNewConnections()
        {
            while (server.IsListening())
            {
                try
                {
                    server.AcceptConnectionIfPending();
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

        static void HandleCurrentConnections()
        {
            HandleValidations();
            HandleReceivingMessages();
            HandleSendingMessages();
        }

        static void HandleValidations()
        {
            while (server.HasPendingValidations())
            {
                logger.LogInfo("Connecting client...");
                bool accepted = server.AcceptNextIfValid();

                if (accepted) logger.LogSuccess("Succesfully connected new client!");
                else logger.LogInfo("Client connection rejected!");
            }
        }

        static void HandleReceivingMessages()
        {
            server.CollectMessagesFromConnectedClients();
        }

        static void HandleSendingMessages()
        {
            while (server.HasPendingMessages())
            {
                Message pendingMessage = server.GetNextPendingMessage();

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
            if (request.messageArg.Equals("disconnect"))
            {
                server.DisconnectClientIfExist(request.messageBody);
            }
        }
    }
}
