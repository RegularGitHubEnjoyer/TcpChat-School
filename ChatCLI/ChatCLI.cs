using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatCLI
{
    public class ChatCLI : CLI
    {
        protected const int BUFFER_SIZE = 3 * 1024; //Buffer size for received message

        protected Socket _socket;

        public ChatCLI(string name = "Chat", string prefix = ">") : base(name, prefix) { }

        public void SendMessage(MessageHeaders header, string body, Socket receiver)
        {
            try
            {
                //format message string with header and content
                string message = $"{header}\n{body}";
                //convert string to bytes
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                //send bytes stored in buffer to receiver socket
                receiver.Send(buffer); 
            }
            catch(SocketException e)
            {
                Log(LogType.Error, $"Error while sending message: {e.Message}");
            }
        }

        public void SendMessage(MessageHeaders header, string argument, string body, Socket receiver)
        {
            try
            {
                //format message string with header and content
                string message = $"{header} {argument}\n{body}";
                //convert string to bytes
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                //send bytes stored in buffer to receiver socket
                receiver.Send(buffer);
            }
            catch (SocketException e)
            {
                Log(LogType.Error, $"Error while sending message: {e.Message}");
            }
        }

        public (MessageHeaders header, string argument, string body) ReceiveMessage(Socket sender)
        {
            //create buffer
            byte[] buffer = new byte[BUFFER_SIZE];
            //store received data in buffer and get the number of bytes received.
            int bytesRead = sender.Receive(buffer);
            //convert bytes to string
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (string.IsNullOrEmpty(message) || message.IndexOf("\n") == -1) //if message is in wrong format
            {
                return (MessageHeaders.Unknown, null, null);
            }

            //get message header, argument and body.
            string headerString = message.Substring(0, message.IndexOf('\n')).Trim();
            string argument = null;
            if (headerString.IndexOf(" ") > -1) //check if message has argument
            {
                argument = headerString.Substring(headerString.IndexOf(" ")).Trim();
                headerString = headerString.Substring(0, headerString.IndexOf(" ")).Trim();
            }

            MessageHeaders header;

            switch (headerString.ToUpper()) //set appropriate header
            {
                case "REQUEST":
                    header = MessageHeaders.Request;
                    break;

                case "RESPONSE":
                    header = MessageHeaders.Response;
                    break;

                case "CONNECTION_STATUS":
                    header = MessageHeaders.Connection_Status;
                    break;

                case "MESSAGE":
                    header = MessageHeaders.Message;
                    break;

                case "PRIVATE_MESSAGE":
                    header = MessageHeaders.Private_Message;
                    break;

                case "ERROR":
                    header = MessageHeaders.Error;
                    break;

                default:
                    header = MessageHeaders.Unknown;
                    break;
            }

            string body = message.Substring(message.IndexOf('\n')).Trim();

            return (header, argument, body);
        }
    }
}
