using Logger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace TcpChatTests
{
    [TestClass]
    public class LoggerTest
    {
        [TestMethod]
        public void LoggerAddsLogsToHistory()
        {
            Logger.Logger logger = new Logger.Logger();

            logger.LogError("");
            logger.LogWarning("");
            logger.LogMessage("");
            logger.LogSuccess("");
            logger.LogInfo("");

            List<Log> history = logger.LogHistory;
            Assert.IsNotNull(history);
            Assert.AreEqual(5, history.Count);
        }

        [TestMethod]
        public void LoggerReturnsCorrectLog()
        {
            Logger.Logger logger = new Logger.Logger();

            logger.LogError("");
            logger.LogWarning("");
            logger.LogMessage("");

            Log error = logger.GetLog(0);
            Log warning = logger.GetLog(1);
            Log message = logger.GetLog(2);

            Assert.AreEqual(LogType.Error, error.GetLogType());
            Assert.AreEqual(LogType.Warning, warning.GetLogType());
            Assert.AreEqual(LogType.Message, message.GetLogType());
        }

        [TestMethod]
        public void LoggerReturnsNLogsIfAvailable()
        {
            Logger.Logger logger = new Logger.Logger();

            logger.LogError("");
            logger.LogWarning("");
            logger.LogMessage("");

            Log[] logs = logger.GetLastNLogsOrLess(3);

            Assert.AreEqual(3, logs.Length);
        }

        public void LoggerReturnsLessThanNLogsIfNotAvailable()
        {
            Logger.Logger logger = new Logger.Logger();

            logger.LogError("");
            logger.LogWarning("");
            logger.LogMessage("");

            Log[] logs = logger.GetLastNLogsOrLess(5);

            Assert.AreNotEqual(5, logs.Length);
            Assert.AreEqual(3, logs.Length);
        }

        public void LoggerReturnsNLogsIfMoreAvailable()
        {
            Logger.Logger logger = new Logger.Logger();

            logger.LogError("");
            logger.LogWarning("");
            logger.LogMessage("");
            logger.LogMessage("");
            logger.LogMessage("");

            Log[] logs = logger.GetLastNLogsOrLess(2);

            Assert.AreEqual(2, logs.Length);
        }

        [TestMethod]
        public void LoggerReturnsCorrectLastNLogs()
        {
            Logger.Logger logger = new Logger.Logger();

            logger.LogError("");
            logger.LogWarning("");
            logger.LogMessage("");
            logger.LogSuccess("");
            logger.LogInfo("");

            Log[] logs = logger.GetLastNLogsOrLess(3);

            Assert.AreEqual(LogType.Info, logs[0].GetLogType());
            Assert.AreEqual(LogType.Success, logs[1].GetLogType());
            Assert.AreEqual(LogType.Message, logs[2].GetLogType());
        }

        [TestMethod]
        public void LoggerClearsLogHistory()
        {
            Logger.Logger logger = new Logger.Logger();

            logger.LogError("");
            logger.LogWarning("");
            logger.LogMessage("");
            logger.LogSuccess("");
            logger.LogInfo("");

            logger.ClearLogsHistory();

            List<Log> history = logger.LogHistory;

            Assert.AreEqual(0, history.Count);
        }
    }
}
