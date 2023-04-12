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
        public string messageArg;
        public string messageBody;

        private Message(MessageHeader messageHeader, string messageArg, string messageBody)
        {
            this.messageHeader = messageHeader;
            this.messageArg = messageArg;
            this.messageBody = messageBody;
        }

        public static Message Request(string messageArg, string messageBody)
        {
            return new Message(MessageHeader.Request, messageArg, messageBody);
        }

        public static Message Response(string messageArg, string messageBody)
        {
            return new Message(MessageHeader.Response, messageArg, messageBody);
        }

        public static Message PublicMessage(string messageArg, string messageBody)
        {
            return new Message(MessageHeader.Public_Message, messageArg, messageBody);
        }

        public static Message PrivateMessage(string messageArg, string messageBody)
        {
            return new Message(MessageHeader.Private_Message, messageArg, messageBody);
        }

        public static Message Error(string messageArg, string messageBody)
        {
            return new Message(MessageHeader.Error, messageArg, messageBody);
        }

        public static Message ConnectionStatus(string messageArg, string messageBody)
        {
            return new Message(MessageHeader.Connection_Status, messageArg, messageBody);
        }

        public static Message Parse(string message)
        {
            return new Message(MessageHeader.Unknown, null, "");
        }

        public override string ToString()
        {
            string argsString = messageArg.Aggregate("", (sum, next) => $"{sum + next} ").Trim();
            return $"{messageHeader} {argsString}\n{messageBody}";
        }
    }
}
