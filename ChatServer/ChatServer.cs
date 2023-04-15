using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using MessageHandler;

namespace ChatServer
{
    internal class ChatServer
    {
        private Socket _listener;
        private bool _isListening;

        private Queue<Socket> _validationQueue;
        private Dictionary<string, Socket> _connectedClients;

        public ChatServer()
        {
            _isListening = false;
            _connectedClients = new Dictionary<string, Socket>();
            _validationQueue = new Queue<Socket>();
        }

        public void Start(IPEndPoint endPoint)
        {
            try
            {
                _initializeSocket(endPoint);
            }
            catch(SocketException e)
            {

            }
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

        public Queue<Message> CollectMessagesFromConnectedClients()
        {
            Queue<Message> messageQueue = new Queue<Message>();
            foreach (string username in _connectedClients.Keys)
            {
                if (_connectedClients[username].Poll(100, SelectMode.SelectRead))
                {
                    if (_connectedClients[username].Available > 0)
                    {
                        Message receivedMessage = MessagingHandler.ReceiveMessage(_connectedClients[username]);
                        messageQueue.Enqueue(receivedMessage);
                    }
                    else
                    {
                        _disconnectClient(username);
                    }
                }
            }

            return messageQueue;
        }

        public void DisconnectClientIfExist(string username)
        {
            if (_connectedClients.ContainsKey(username)) _disconnectClient(username);
        }

        public void BroadcastMessage(Message message)
        {
            foreach (string username in _connectedClients.Keys)
            {
                if (message.HasArgument("sender") && message.GetArgumentValue("sender") == username) continue;

                MessagingHandler.SendMessage(message, _connectedClients[username]);
            }
        }

        public void SendPrivateMessage(Message message)
        {
            if (message.HasArgument("receiver"))
            {
                string username = message.GetArgumentValue("receiver");

                if (_connectedClients.ContainsKey(username))
                {
                    MessagingHandler.SendMessage(message, _connectedClients[username]);
                }
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
                Message usernameRequest = Message.Request("Username");
                MessagingHandler.SendMessage(usernameRequest, connection);

                Message clientResponse = MessagingHandler.ReceiveMessage(connection);
                if (clientResponse.messageHeader == MessageHeader.Response && clientResponse.HasArgument("username"))
                {
                    username = clientResponse.GetArgumentValue("username");
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
