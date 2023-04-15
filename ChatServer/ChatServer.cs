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

        private Dictionary<string, Socket> _connectedClients;

        public ChatServer()
        {
            _isListening = false;
            _connectedClients = new Dictionary<string, Socket>();
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

        public bool HasPendingConnection()
        {
            return _listener.Poll(100, SelectMode.SelectRead);
        }

        public Socket AcceptConnection()
        {
            return _listener.Accept();
        }

        public bool IsClientConnected(string username)
        {
            return _connectedClients.ContainsKey(username);
        }

        public void AddClientConnection(string username, Socket connection)
        {
            _connectedClients.Add(username, connection);
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
                        receivedMessage.AddArgument("sender", username);

                        messageQueue.Enqueue(receivedMessage);
                    }
                }
            }

            return messageQueue;
        }

        public void DisconnectClient(string username)
        {
            _connectedClients[username].Close(5);
            _connectedClients.Remove(username);
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
    }
}
