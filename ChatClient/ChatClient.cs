using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ChatClient
{
    internal class ChatClient
    {
        private Socket _client;
        private bool _isConnected;

        public ChatClient()
        {
            _isConnected = false;
        }

        public void Connect(IPEndPoint endPoint)
        {

        }

        public void Disconnect()
        {

        }

        public bool IsConnected()
        {
            return _isConnected;
        }

        public void CollectMessagesFromServer()
        {
            
        }
    }
}
