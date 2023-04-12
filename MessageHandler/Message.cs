using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageHandler
{
    public class Message
    {
        public MessageHeader messageHeader;
        public string[] messageArgs;
        public string messageBody;

        private Message(MessageHeader messageHeader, string[] messageArgs, string messageBody)
        {
            this.messageHeader = messageHeader;
            this.messageArgs = messageArgs;
            this.messageBody = messageBody;
        }

        public static Message Request(string[] messageArgs, string messageBody)
        {
            return new Message(MessageHeader.Request, messageArgs, messageBody);
        }

        public static Message Response(string[] messageArgs, string messageBody)
        {
            return new Message(MessageHeader.Response, messageArgs, messageBody);
        }

        public static Message PublicMessage(string[] messageArgs, string messageBody)
        {
            return new Message(MessageHeader.Public_Message, messageArgs, messageBody);
        }

        public static Message PrivateMessage(string[] messageArgs, string messageBody)
        {
            return new Message(MessageHeader.Private_Message, messageArgs, messageBody);
        }

        public static Message Error(string[] messageArgs, string messageBody)
        {
            return new Message(MessageHeader.Error, messageArgs, messageBody);
        }

        public static Message ConnectionStatus(string[] messageArgs, string messageBody)
        {
            return new Message(MessageHeader.Connection_Status, messageArgs, messageBody);
        }

        public static Message Parse(string message)
        {
            return new Message(MessageHeader.Unknown, null, "");
        }

        public override string ToString()
        {
            string argsString = messageArgs.Aggregate("", (sum, next) => $"{sum + next} ").Trim();
            return $"{messageHeader} {argsString}\n{messageBody}";
        }
    }
}
