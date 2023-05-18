using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Utility.Messaging;

namespace ChatClient
{
    public class ChatClient
    {
        private Socket _client;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public ChatClient()
        {
            _isConnected = false;
        }

        public void Connect(IPEndPoint endPoint)
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _client.Connect(endPoint);
            _client.SendTimeout = 5 * 1000;
            _client.ReceiveTimeout = 5 * 1000;
            _isConnected = true;
        }

        public void RequestDisconnect()
        {
            Message disconnectRequest = Message.Request("Disconnect me!");
            disconnectRequest.AddArgument("request", "disconnect");

            MessagingHandler.SendMessage(disconnectRequest, _client);
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                _client.Close(5);
                _isConnected = false;
                _client = null;
            }
        }

        public Queue<Message> GetPendingMessages()
        {
            Queue<Message> pendingMessages = new Queue<Message>();

            while (_hasPendingMessage())
            {
                Message receivedMessage = MessagingHandler.ReceiveMessage(_client);
                pendingMessages.Enqueue(receivedMessage);
            }

            return pendingMessages;
        }

        public void SendChatMessage(string message)
        {
            Message chatMessage = Message.PublicMessage(message);
            SendMessage(chatMessage);
        }

        public void SendMessage(Message message)
        {
            MessagingHandler.SendMessage(message, _client);
        }

        public bool LostConnectionWithServer()
        {
            try
            {
                return _client.Poll(100, SelectMode.SelectRead) && _client.Available == 0;
            }
            catch (Exception e) when (e is ObjectDisposedException || e is NullReferenceException)
            {
                return false;
            }
        }

        private bool _hasPendingMessage()
        {
            return _isConnected && _client.Poll(100, SelectMode.SelectRead) && _client.Available > 0;
        }
    }
}
