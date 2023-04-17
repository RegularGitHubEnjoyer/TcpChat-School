using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    internal class Client
    {
        public Socket ClientSocket { get; private set; }
        public IPEndPoint RemoteEndpoint { get; private set; }
        public string Username { get; private set; }

        public bool IsConnected
        {
            get
            {
                return !((ClientSocket.Poll(1000, SelectMode.SelectRead) && (ClientSocket.Available == 0)) || !ClientSocket.Connected);
            }
        }

        public Client(Socket clientSocket, string username)
        {
            ClientSocket = clientSocket;
            RemoteEndpoint = (IPEndPoint)ClientSocket.RemoteEndPoint;
            Username = username;
        }
    }
}
