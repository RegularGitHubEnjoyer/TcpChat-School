using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class Logger
    {
        private List<Log> _logHistory;

        public List<Log> LogHistory => _logHistory;
        public event Action LogHistoryChanged;

        public Logger()
        {
            _logHistory = new List<Log>();
        }

        public Log[] GetLastNLogsOrLess(int nLastLogs)
        {
            int size = nLastLogs;
            if(size > _logHistory.Count) size = _logHistory.Count;

            Log[] logs = new Log[size];
            for (int i = 0; i < size; i++)
            {
                logs[i] = GetLog(_logHistory.Count - 1 - i);
            }
            return logs;
        }

        public Log GetLog(int requiredLog)
        {
            return _logHistory[requiredLog];
        }

        public void ClearLogsHistory()
        {
            _logHistory.Clear();
            LogHistoryChanged?.Invoke();
        }

        public void LogMessage(string message)
        {
            Log newLog = new Log(LogType.Message, message);
            _addNewLogToHistory(newLog);
        }

        public void LogSuccess(string message)
        {
            Log newLog = new Log(LogType.Success, message);
            _addNewLogToHistory(newLog);
        }

        public void LogInfo(string message)
        {
            Log newLog = new Log(LogType.Info, message);
            _addNewLogToHistory(newLog);
        }

        public void LogWarning(string message)
        {
            Log newLog = new Log(LogType.Warning, message);
            _addNewLogToHistory(newLog);
        }

        public void LogError(string message)
        {
            Log newLog = new Log(LogType.Error, message);
            _addNewLogToHistory(newLog);
        }

        private void _addNewLogToHistory(Log newLog)
        {
            _logHistory.Add(newLog);
            LogHistoryChanged?.Invoke();
        }
    }
}
