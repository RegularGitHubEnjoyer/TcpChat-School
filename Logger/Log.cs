using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class Log
    {
        private string _timestamp;
        private string _message;
        private LogType _type;

        public Log(LogType type, string message)
        {
            _type = type;
            _message = message;
            _timestamp = DateTime.Now.ToLongTimeString();
        }

        public string GetMessageWithTimeStamp()
        {
            return $"[{_timestamp}] {_message}";
        }

        public string GetMessage()
        {
            return _message;
        }

        public LogType GetLogType()
        {
            return _type;
        }
    }
}
