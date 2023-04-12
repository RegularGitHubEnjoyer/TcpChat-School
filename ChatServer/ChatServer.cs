using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessageHandler;

namespace ChatServer
{
    internal class ChatServer
    {
        private Socket _listener;
        private bool _isListening;

        private Queue<Socket> _validationQueue;
        private Dictionary<string, Socket> _connectedClients;

        private Queue<Message> _messageQueue;

        public ChatServer()
        {
            _isListening = false;
            _connectedClients = new Dictionary<string, Socket>();
            _validationQueue = new Queue<Socket>();
            _messageQueue = new Queue<Message>();
        }

        public void Start(IPEndPoint endPoint)
        {
            _initializeSocket(endPoint);

            Thread connectionListenThread = new Thread(_handleListenForNewConnection);
            connectionListenThread.Start();

            Thread clientConnectionThread = new Thread(_handleConnections);
            clientConnectionThread.Start();
        }

        public void Stop()
        {
            if (_isListening)
            {
                _isListening = false;
                _listener.Close(5);
                _listener = null;
                _connectedClients.Clear();
            }
        }

        private void _initializeSocket(IPEndPoint endPoint)
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(endPoint);
            _listener.Listen(10);
            _isListening = true;
        }

        private void _handleListenForNewConnection()
        {
            while (_isListening)
            {
                if(_listener.Poll(100, SelectMode.SelectRead))
                {
                    Socket connection = _listener.Accept();
                    _validationQueue.Enqueue(connection);
                }
            }
        }

        private void _handleConnections()
        {
            while (_isListening)
            {
                _acceptValidConnections();
                _receiveMessages();
                _handleMessages();
            }
        }

        private void _acceptValidConnections()
        {
            while(_validationQueue.Count > 0)
            {
                Socket connection = _validationQueue.Dequeue();
                string username = _requestUsername(connection);

                if (_requestedUsernameIsValid(username))
                {
                    _acceptClientWithUsername(connection, username);
                }
                else
                {
                    connection.Close(5);
                }
            }
        }

        private string _requestUsername(Socket connection)
        {
            connection.ReceiveTimeout = 5 * 1000;
            connection.SendTimeout = 5 * 1000;

            string username = "";

            try
            {
                Message usernameRequest = Message.Request("LoginInfo", "Username");
                MessagingHandler.SendMessage(usernameRequest, connection);

                Message clientResponse = MessagingHandler.ReceiveMessage(connection);
                if(clientResponse.messageHeader == MessageHeader.Response && clientResponse.messageArg.Equals("Username", StringComparison.OrdinalIgnoreCase))
                {
                    username = clientResponse.messageBody;
                }
            }
            catch (SocketException e)
            {
                
            }
            finally
            {
                connection.ReceiveTimeout = 0;
                connection.SendTimeout = 0;
            }

            return username;
        }

        private bool _requestedUsernameIsValid(string username)
        {
            return !string.IsNullOrEmpty(username) && !_connectedClients.ContainsKey(username);
        }

        private void _acceptClientWithUsername(Socket client, string username)
        {
            _connectedClients.Add(username, client);
            //Send connection status message
        }

        private void _receiveMessages()
        {
            foreach (string username in _connectedClients.Keys)
            {
                if (_connectedClients[username].Poll(100, SelectMode.SelectRead))
                {
                    if (_connectedClients[username].Available > 0)
                    {
                        Message receivedMessage = MessagingHandler.ReceiveMessage(_connectedClients[username]);
                        _messageQueue.Enqueue(receivedMessage);
                    }
                    else
                    {
                        _disconnectClient(username);
                    }
                }
            }
        }

        private void _disconnectClient(string username)
        {
            _connectedClients[username].Close(5);
            _connectedClients.Remove(username);
        }

        private void _handleMessages()
        {
            while(_messageQueue.Count > 0)
            {
                Message message = _messageQueue.Dequeue();

                switch (message.messageHeader)
                {
                    case MessageHeader.Request:
                        _handleRequest(message);
                        break;
                    case MessageHeader.Public_Message:
                        _broadcastMessage(message);
                        break;
                    case MessageHeader.Private_Message:
                        _sendPrivateMessage(message);
                        break;
                }
            }
        }

        private void _handleRequest(Message request)
        {
            if (request.messageArg.Equals("disconnect"))
            {
                _disconnectClient(request.messageBody);
            }
        }

        private void _broadcastMessage(Message message)
        {
            foreach (string username in _connectedClients.Keys)
            {
                if (username == message.messageArg) continue;

                MessagingHandler.SendMessage(message, _connectedClients[username]);
            }
        }

        private void _sendPrivateMessage(Message message)
        {
            string[] usernames = message.messageArg.Split(' ');

            if (usernames.Length > 2 && _connectedClients.ContainsKey(usernames[1]))
            {
                MessagingHandler.SendMessage(message, _connectedClients[usernames[1]]);
            }
        }
    }
}
