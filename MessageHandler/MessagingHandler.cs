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
        private const int BUFFER_SIZE = 3 * 1024;

        public static async Task SendMessageAsync(Message message, Socket receiver)
        {
            string messageString = message.ToString();
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageString));
            await receiver.SendAsync(buffer, SocketFlags.None);
        }

        public static async Task<Message> ReceiveMessageAsync(Socket sender)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[BUFFER_SIZE]);
            int bytesReceived = await sender.ReceiveAsync(buffer, SocketFlags.None);
            string messageString = Encoding.UTF8.GetString(buffer.ToArray(), 0, bytesReceived);
            return Message.Parse(messageString);
        }
    }
}
