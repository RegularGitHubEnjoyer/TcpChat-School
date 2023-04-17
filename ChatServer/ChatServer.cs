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

        private Dictionary<string, Client> _connectedClients;

        public bool IsListening => _isListening;

        public ChatServer()
        {
            _isListening = false;
            _connectedClients = new Dictionary<string, Client>();
        }

        public void Start(IPEndPoint endPoint)
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(endPoint);
            _listener.Listen(10);
            _isListening = true;
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

        public bool HasPendingConnection()
        {
            return IsListening && _listener.Poll(100, SelectMode.SelectRead);
        }

        public Socket AcceptConnection()
        {
            return _listener.Accept();
        }

        public bool IsClientConnectedWithUsername(string username)
        {
            return _connectedClients.ContainsKey(username);
        }

        public Client GetClientByUsername(string username)
        {
            return _connectedClients[username];
        }

        public string[] GetClientsUsernames()
        {
            return _connectedClients.Keys.ToArray();
        }

        public void AddClientConnection(string username, Client connection)
        {
            _connectedClients.Add(username, connection);
        }

        public Queue<Message> CollectMessagesFromClients()
        {
            Queue<Message> messageQueue = new Queue<Message>();
            foreach (string username in GetClientsUsernames())
            {
                if (_connectedClients[username].ClientSocket.Poll(100, SelectMode.SelectRead))
                {
                    if (_connectedClients[username].ClientSocket.Available > 0)
                    {
                        Message receivedMessage = MessagingHandler.ReceiveMessage(_connectedClients[username].ClientSocket);
                        receivedMessage.AddArgument("sender", username);

                        messageQueue.Enqueue(receivedMessage);
                    }
                }
            }

            return messageQueue;
        }

        public void DisconnectClient(string username)
        {
            _connectedClients[username].ClientSocket.Close(5);
            _connectedClients.Remove(username);
        }

        public void BroadcastMessage(Message message)
        {
            foreach (string username in _connectedClients.Keys.ToArray())
            {
                if ((message.HasArgument("sender") && message.GetArgumentValue("sender") == username) || !_connectedClients[username].IsConnected) continue;

                MessagingHandler.SendMessage(message, _connectedClients[username].ClientSocket);
            }
        }

        public void SendMessage(Message message)
        {
            if (message.HasArgument("receiver"))
            {
                string username = message.GetArgumentValue("receiver");

                if (_connectedClients.ContainsKey(username))
                {
                    MessagingHandler.SendMessage(message, _connectedClients[username].ClientSocket);
                }
            }
        }

        public List<Client> GetClientsWhoLostConnection()
        {
            return _connectedClients.Values.Where(client => !client.IsConnected).ToList();
        }
    }
}
