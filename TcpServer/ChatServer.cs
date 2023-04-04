using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ChatCLI;
using System.Net;
using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace TcpServer
{
    public class ChatServer : ChatCLI.ChatCLI
    {
        private Dictionary<string, Socket> _connectedClients;

        private bool _serverIsUp;
        public bool IsServerRunning => _serverIsUp;

        public ChatServer(string name = "Chat Server", string prefix = ">") : base(name, prefix)
        {
            _connectedClients = new Dictionary<string, Socket>();
        }

        public void Start(IPEndPoint endPoint)
        {
            //Start can only be invoked if server isn't currently running.
            if (_socket != null || _serverIsUp)
            {
                Log(LogType.Warning, $"Server is already running!");
                return;
            }
            Log(LogType.Information, $"Starting server @ {endPoint.Address}:{endPoint.Port}...");

            try
            {
                //create new tcp/ip socket, bind it to given endpoint and listen for connection with max 10 pending connections in queue.
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Bind(endPoint);
                _socket.Listen(10);

                //set flag for keeping server up to true.
                _serverIsUp = true;

                //create new thread to listen for clients.
                Thread listenForConnectionThread = new Thread(() => _handleListenForConnection());
                listenForConnectionThread.Start();
                Log(LogType.Success, $"Succesfully started server!");
            }
            catch(SocketException e)
            {
                Log(LogType.Error, $"Error while starting server: {e.Message}");
            }
        }

        public void StopServer()
        {
            //Stop can only be invoked if server is running.

            if (_socket == null)
            {
                Log(LogType.Warning, "Server has already stopped!");
                return;
            }
            Log(LogType.Information, "Stopping server...");


            try
            {
                //set flag for keeping server up to false.
                _serverIsUp = false;

                //disconnect all connected clients
                foreach (string client in _connectedClients.Keys.ToArray())
                {
                    _connectedClients[client].Close(5);
                    _connectedClients.Remove(client);
                }

                //close server socket with 5 sec timeout to allow queued data to be sent.
                _socket.Close(5);
                _socket = null;
            }
            catch(SocketException e)
            {
                Log(LogType.Error, $"Error while stopping server: {e.Message}");
            }
            finally
            {
                Log(LogType.Information, "Server stopped!");
            }
        }

        public override void Stop()
        {
            StopServer();
            base.Stop();
        }

        private void _handleListenForConnection()
        {
            try
            {
                //Listen for connection only if server is up.
                while (_serverIsUp)
                {
                    //wait 1 ms for a connection.
                    if (_socket.Poll(1000, SelectMode.SelectRead) && _serverIsUp) //checking serverUp flag 'cause poll returns true also if the connection has been closed, reset, or terminated.
                    {
                        Log(LogType.Information, "Connecting new client...");

                        //Accept new connection and get socket coresponding to that connection.
                        Socket connection = _socket.Accept();

                        //create thread to handle connection with new client.
                        Thread handleConnectionThread = new Thread(() => _handleClientConnection(connection));
                        handleConnectionThread.Start();
                    }
                }
            }
            catch (Exception e)
            {
                Log(LogType.Error, $"Error occured while server was running: {e.Message}\n");
                //stop server if error occured while server was running
                StopServer();
            }
        }

        private void _handleClientConnection(Socket clientSocket)
        {

            //initialize current client data
            string clientUsername = "";
            string disconnectReason = "Server closed";
            bool isConnectionValid = false;

            try
            {
                //validate connection
                (isConnectionValid, clientUsername, disconnectReason) = _validateConnection(clientSocket, 5 * 1000);
                if (isConnectionValid)
                {
                    Log(LogType.Success, $"Succesfully connected client with username: {clientUsername}");
                    SendMessage(MessageHeaders.Connection_Status, "Established", "Connection established succesfully!", clientSocket);
                }
                else
                {
                    SendMessage(MessageHeaders.Connection_Status, "Denied", $"Connection denied: {disconnectReason}", clientSocket);
                }

                //handle messaging if connection is valid and server is up
                while (isConnectionValid && _serverIsUp)
                {
                    if(clientSocket.Poll(1000, SelectMode.SelectRead))
                    {
                        if(clientSocket.Available > 0)
                        {
                            //break down message to header, argument and body
                            var (header, argument, body) = ReceiveMessage(clientSocket);

                            switch (header) //handle message based on header
                            {
                                case MessageHeaders.Request:

                                    if(argument == null)
                                    {
                                        SendMessage(MessageHeaders.Error, $"Requests require argument!", clientSocket);
                                    }
                                    else if(argument.Equals("Disconnect", StringComparison.OrdinalIgnoreCase))
                                    {
                                        isConnectionValid = false;
                                        disconnectReason = "Disconnect Requested";
                                        SendMessage(MessageHeaders.Connection_Status, "Disconnected", "Succesfully disconnected", clientSocket);
                                    }
                                    else
                                    {
                                        SendMessage(MessageHeaders.Error, $"Unknown request: {argument}", clientSocket);
                                    }

                                    break;

                                case MessageHeaders.Message:
                                    Log($"<{clientUsername}> {body}");
                                    foreach(string client in _connectedClients.Keys.ToArray())
                                    {
                                        if(clientUsername.Equals(client, StringComparison.OrdinalIgnoreCase)) continue;

                                        SendMessage(MessageHeaders.Message, clientUsername, body, _connectedClients[client]);
                                    }
                                    break;

                                case MessageHeaders.Private_Message:
                                    if (argument == null)
                                    {
                                        SendMessage(MessageHeaders.Error, $"Private messages require argument!", clientSocket);
                                    }
                                    else if (_connectedClients.ContainsKey(argument))
                                    {
                                        Log($"<%{clientUsername} -> {argument}%> {body}");
                                        SendMessage(MessageHeaders.Private_Message, clientUsername, body, _connectedClients[argument]);
                                    }
                                    else
                                    {
                                        SendMessage(MessageHeaders.Error, $"There is no client: {argument}", clientSocket);
                                    }
                                    break;

                                case MessageHeaders.Unknown:
                                    SendMessage(MessageHeaders.Error, $"Unknown header", clientSocket);
                                    break;

                                default:
                                    SendMessage(MessageHeaders.Error, $"Unhandled header", clientSocket);
                                    break;
                            }
                        }
                        else
                        {
                            disconnectReason = "Connection lost";
                            isConnectionValid = false;
                        }
                    }
                }
            }
            catch(SocketException e)
            {
                Log(LogType.Error, e.Message);
            }
            catch(ObjectDisposedException e)
            {
                if (!_serverIsUp) disconnectReason = "Server closed";
                else disconnectReason = "Client socket disposed for unknown reason";
            }
            finally
            {
                //disconnect client
                if (_connectedClients.ContainsKey(clientUsername)) _connectedClients.Remove(clientUsername);
                Log(LogType.Information, $"Client {clientUsername} disconnected: {disconnectReason}");
                if(clientSocket != null) clientSocket.Close(5);
            }
        }

        private (bool, string, string) _validateConnection(Socket connection, int timeout = 1000)
        {
            //initialize validation variables
            string clientUsername = "";
            string disconnectReason = "Connection Lost";
            bool isConnectionValid = false;

            try
            {
                //send request for username
                SendMessage(MessageHeaders.Request, "LoginInfo", "Username", connection);

                //wait timeout [ms] for response
                if (connection.Poll(timeout * 1000, SelectMode.SelectRead))
                {
                    if (connection.Available > 0)
                    {
                        //break down message to header, argument and body
                        var (header, argument, body) = ReceiveMessage(connection);

                        //check if response is correct and username is available
                        if (header == MessageHeaders.Response && argument.Equals("username", StringComparison.OrdinalIgnoreCase))
                        {
                            clientUsername = body;

                            if (_connectedClients.ContainsKey(clientUsername))
                            {
                                disconnectReason = "Invalid Username";
                            }
                            else
                            {
                                _connectedClients.Add(clientUsername, connection);
                                isConnectionValid = true;
                            }
                        }
                        else
                        {
                            disconnectReason = "Invalid Connection Headers";
                        }
                    }
                }
                else
                {
                    disconnectReason = "Timed Out";
                }
            }
            catch(SocketException e)
            {
                Log(LogType.Error, $"Error while validating client: {e.Message}");
            }
            
            return (isConnectionValid, clientUsername, disconnectReason);
        }
    }
}
