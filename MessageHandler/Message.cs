using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageHandler
{
    public class Message
    {
        private Dictionary<string, string> _messageArgs;

        public MessageHeader messageHeader;
        public string messageBody;

        private Message(MessageHeader messageHeader, string messageBody)
        {
            _messageArgs = new Dictionary<string, string>();

            this.messageHeader = messageHeader;
            this.messageBody = messageBody;
        }

        public static Message Request(string messageBody)
        {
            return new Message(MessageHeader.Request, messageBody);
        }

        public static Message Response(string messageBody)
        {
            return new Message(MessageHeader.Response, messageBody);
        }

        public static Message PublicMessage(string messageBody)
        {
            return new Message(MessageHeader.Public_Message, messageBody);
        }

        public static Message PrivateMessage(string messageBody)
        {
            return new Message(MessageHeader.Private_Message, messageBody);
        }

        public static Message Error(string messageBody)
        {
            return new Message(MessageHeader.Error, messageBody);
        }

        public static Message ConnectionStatus( string messageBody)
        {
            return new Message(MessageHeader.Connection_Status, messageBody);
        }

        public static Message Server(string messageBody)
        {
            return new Message(MessageHeader.Server, messageBody);
        }

        public static Message Parse(string messageString)
        {
            string[] messageParts = messageString.Split('\n');

            string messageHeaderString = messageParts[0].Substring(0, messageParts[0].IndexOf(' ')).Trim();
            string[] messageArgs = messageParts[0].Substring(messageParts[0].IndexOf(' ')).Trim().Split(',');
            string messageBody = messageParts[1].Trim();

            MessageHeader messageHeader;

            try
            {
                messageHeader = (MessageHeader)Enum.Parse(typeof(MessageHeader), messageHeaderString);
            }
            catch (Exception e) when (e is ArgumentNullException || e is ArgumentException || e is OverflowException)
            {
                messageHeader = MessageHeader.Unknown;
            }

            Message message = new Message(messageHeader, messageBody);

            foreach (string arg in messageArgs)
            {
                if(arg.IndexOf('=') != -1)
                {
                    string argName = arg.Substring(0, arg.IndexOf('='));
                    string argValue = arg.Substring(arg.IndexOf('=') + 1);

                    message.AddArgument(argName, argValue);
                }
            }

            return message;
        }

        public void AddArgument(string argName, object value)
        {
            _messageArgs.Add(argName, value.ToString());
        }

        public bool HasArgument(string argName)
        {
            return _messageArgs.Keys.Any(arg => arg.ToLower() == argName.ToLower());
        }

        public string[] GetArgumentsNames()
        {
            return _messageArgs.Keys.ToArray();
        }

        public string GetArgumentValue(string argName)
        {
            return _messageArgs.First(arg => arg.Key.Equals(argName, StringComparison.OrdinalIgnoreCase)).Value;
        }

        public int GetNumberOfArguments()
        {
            return _messageArgs.Count;
        }

        public override string ToString()
        {
            string argsString = _messageArgs.Aggregate("", (sum, next) => $"{sum} {next.Key}={next.Value}").Trim().Replace(' ', ',');
            return $"{messageHeader} {argsString}\n{messageBody}";
        }
    }
}
