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

        public bool IsListening()
        {
            return _isListening;
        }

        public void AcceptConnectionIfPending()
        {
            if (_listener.Poll(100, SelectMode.SelectRead))
            {
                Socket connection = _listener.Accept();
                _validationQueue.Enqueue(connection);
            }
        }

        public bool HasPendingValidations()
        {
            return _validationQueue.Count > 0;
        }

        public bool AcceptNextIfValid()
        {
            Socket connection = _validationQueue.Dequeue();
            string username = _requestUsername(connection);

            if (_requestedUsernameIsValid(username))
            {
                _acceptClientWithUsername(connection, username);
                return true;
            }
            else
            {
                connection.Close(5);
                return false;
            }
        }

        public void CollectMessagesFromConnectedClients()
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

        public bool HasPendingMessages()
        {
            return _messageQueue.Count > 0;
        }

        public Message GetNextPendingMessage()
        {
            return _messageQueue.Dequeue();
        }

        public void DisconnectClientIfExist(string username)
        {
            if (_connectedClients.ContainsKey(username)) _disconnectClient(username);
        }

        public void BroadcastMessage(Message message)
        {
            foreach (string username in _connectedClients.Keys)
            {
                if (username == message.messageArg) continue;

                MessagingHandler.SendMessage(message, _connectedClients[username]);
            }
        }

        public void SendPrivateMessage(Message message)
        {
            string[] usernames = message.messageArg.Split(' ');

            if (usernames.Length > 2 && _connectedClients.ContainsKey(usernames[1]))
            {
                MessagingHandler.SendMessage(message, _connectedClients[usernames[1]]);
            }
        }

        private void _initializeSocket(IPEndPoint endPoint)
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(endPoint);
            _listener.Listen(10);
            _isListening = true;
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

        private void _disconnectClient(string username)
        {
            _connectedClients[username].Close(5);
            _connectedClients.Remove(username);
        }
    }
}
