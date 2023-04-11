using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessageHandler
{
    public static class MessagingHandler
    {
        public static void SendMessage(Message message, Socket receiver)
        {
            try
            {
                string messageString = message.ToString();
                byte[] buffer = Encoding.UTF8.GetBytes(messageString);
                receiver.Send(buffer);
            }
            catch (SocketException e)
            {
                throw new Exception("Unable to send message!: " + e.Message);
            }
        }

        public static Message ReceiveMessage(Socket sender)
        {
            try
            {
                byte[] buffer = new byte[3 * 1024];
                int bytesReceived = sender.Receive(buffer);
                string messageString = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                return Message.Parse(messageString);
            }
            catch(SocketException e)
            {
                throw new Exception("Unable to receive message!: " + e.Message);
            }
        }
    }
}
