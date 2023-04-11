using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConnectionHandler
{
    public class ConnectionHandler
    {
        private enum Mode
        {
            None,
            Listener,
            Client
        }

        private Socket _socket;
        private Mode _mode;

        public ConnectionHandler()
        {
            _mode = Mode.None;
        }

        public void StartAsListener(IPEndPoint endPoint)
        {
            if(_mode == Mode.None)
            {
                try
                {
                    InitSocket();
                    _socket.Bind(endPoint);
                    _socket.Listen(10);

                    _mode = Mode.Listener;
                }
                catch(SocketException e)
                {
                    throw new Exception("Unable to start as a listener!");
                }
            }
        }

        public void StartAsClient(IPEndPoint endPoint)
        {
            if(_mode == Mode.None)
            {
                try
                {
                    InitSocket();
                    _socket.Connect(endPoint);

                    _mode = Mode.Client;
                }
                catch (SocketException e)
                {
                    throw new Exception("Unable to start as a client!");
                }
            }
        }

        public void Stop()
        {
            if(_socket != null)
            {
                _socket.Close(5);
                _socket = null;
            }

            _mode = Mode.None;
        }

        public Socket AwaitConnection(int timeout)
        {
            if(_mode == Mode.Listener && _socket.Poll(timeout, SelectMode.SelectRead))
            {
                if(_socket.Available > 0)
                {
                    return _socket.Accept();
                }
                Stop();
                throw new Exception("Socket lost connection!");
            }
            return null;
        }

        public Socket GetSocket()
        {
            return _socket;
        }

        public bool IsConnected()
        {
            return _mode != Mode.None;
        }

        private void InitSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}
