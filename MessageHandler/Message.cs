using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageHandler
{
    public class Message
    {
        private MessageHeader _messageHeader;
        private string[] _messageArgs;
        private string _messageBody;

        private Message(MessageHeader messageHeader, string[] messageArgs, string messageBody)
        {
            _messageHeader = messageHeader;
            _messageArgs = messageArgs;
            _messageBody = messageBody;
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
            string argsString = _messageArgs.Aggregate("", (sum, next) => $"{sum + next} ").Trim();
            return $"{_messageHeader} {argsString}\n{_messageBody}";
        }
    }
}
