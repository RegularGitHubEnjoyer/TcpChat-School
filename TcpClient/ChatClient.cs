using ChatCLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpClient
{
    public class ChatClient : ChatCLI.ChatCLI
    {
        private bool _connected;
        private string _username;

        public ChatClient(string name = "Chat Client", string prefix = ">") : base(name, prefix) { }

        public void Connect(IPEndPoint endPoint, string username)
        {
            //Connect client only if isn't currently connected
            if(_connected || _socket != null) 
            {
                Log(LogType.Warning, "Socket is already connected!");
            }

            try
            {
                //create socket and connect to endpoint
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(endPoint);

                //assign username and set connected flag to true
                _username = username;
                _connected = true;

                //start thread to handle connection
                Thread connectionThread = new Thread(_handleConnection);
                connectionThread.Start();
            }
            catch(SocketException e)
            {
                Log(LogType.Error, $"Error while trying to connect to server: {e.Message}");
            }
        }

        public override void Stop()
        {
            _disconnect();
            base.Stop();
        }

        //wrapper for senmessage to send disconnect message
        public void RequestDisconnect() => SendMessage(MessageHeaders.Request, "Disconnect", "", _socket);

        //wrapper for send message to send chat messages
        public void SendChatMessage(string message)
        {
            //only allow to send messages with text in them
            if (string.IsNullOrEmpty(message))
            {
                Log(LogType.Warning, "Cannot send empty messages!");
            }
            else
            {
                Log(message);
                SendMessage(MessageHeaders.Message, message, _socket);
            }
        }

        //wrapper for send message to send private chat messages
        public void SendPrivateChatMessage(string receiver, string message)
        {
            //only allow to send messages with text in them
            if (string.IsNullOrEmpty(message))
            {
                Log(LogType.Warning, "Cannot send empty messages!");
            }
            else
            {
                Log($"{receiver} <- {message}");
                SendMessage(MessageHeaders.Private_Message, receiver, message, _socket);
            }
        }

        //wrapper for send message to send requests
        public void SendRequest(string request, string message = "")
        {
            //only allow to send messages with text in them
            if (string.IsNullOrEmpty(request))
            {
                Log(LogType.Warning, "Cannot send empty Request!");
            }
            else
            {
                SendMessage(MessageHeaders.Request, request, message, _socket);
            }
        }

        private void _disconnect()
        {
            //set connected flag to false
            _connected = false;
            try
            {
                if(_socket != null) 
                {
                    //disconnect and reset socket
                    _socket.Close(5);
                    _socket = null;
                }
            }
            catch (SocketException e)
            {
                Log(LogType.Error, $"Error while trying to disconnect from server: {e.Message}");
            }
        }

        private void _handleConnection()
        {
            try
            {
                //set handler for text input
                InputHandler += SendChatMessage;

                //handle messaging while connected to a server
                while (_connected)
                {
                    if(_socket.Poll(1000, SelectMode.SelectRead) && _connected) //check if data is ready to read
                    {
                        if(_socket.Available > 0) //if number of bytes is greater than 0 message was send
                        {
                            //break down message to header, argument and body
                            var (header, argument, body) = ReceiveMessage(_socket);

                            switch (header) //handle messege based on header
                            {
                                case MessageHeaders.Request:
                                    if(argument.Equals("LoginInfo", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if(body.Equals("Username", StringComparison.OrdinalIgnoreCase))
                                        {
                                            SendMessage(MessageHeaders.Response, "Username", _username, _socket);
                                        }
                                    }
                                    break;

                                case MessageHeaders.Response:
                                    //TODO
                                    break;

                                case MessageHeaders.Connection_Status:
                                    if (argument.Equals("Established", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Log(LogType.Success, body);
                                    }
                                    else if(argument.Equals("Denied", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Log(LogType.Warning, body);
                                    }
                                    else if (argument.Equals("Disconnected", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Log(LogType.Success, body);
                                        _connected = false;
                                    }
                                    break;

                                case MessageHeaders.Message:
                                    if(argument == null)
                                    {
                                        Log($"?ANONYMOUS? {body}");
                                    }
                                    else
                                    {
                                        Log($"<{argument}> {body}");
                                    }
                                    break;

                                case MessageHeaders.Private_Message:
                                    if (argument == null)
                                    {
                                        Log(LogType.Information, $"?%ANONYMOUS%? {body}");
                                    }
                                    else
                                    {
                                        Log(LogType.Information, $"<%{argument}%> {body}");
                                    }
                                    break;

                                case MessageHeaders.Error:
                                    Log(LogType.Error, $"Server error: {body}");
                                    break;

                                case MessageHeaders.Unknown:
                                    //TODO
                                    break;

                                default:
                                    //TODO
                                    break;
                            }
                        }
                        else //if number of bytes is 0 socket lost connection
                        {
                            Log(LogType.Warning, "Disconnected. Lost connection with server");
                            break;
                        }
                    }
                }
            }
            catch(SocketException e)
            {
                Log(LogType.Error, $"Error during connection: {e.Message}");
            }
            finally
            {
                //clear input handler delegate
                InputHandler -= SendChatMessage;
                _disconnect();
            }
        }
    }

}
