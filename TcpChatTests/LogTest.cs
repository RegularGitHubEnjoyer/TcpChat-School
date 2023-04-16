using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Logger;

namespace TcpChatTests
{
    [TestClass]
    public class LogTest
    {
        [TestMethod]
        public void LogReturnsCorrectMessage()
        {
            string expectedString = "test message";
            Log log = new Log(LogType.Message, expectedString);

            Assert.AreEqual(expectedString, log.GetMessage());
        }

        [TestMethod]
        public void LogReturnsCorrectMessageWithTimeStamp()
        {
            string expectedString = "test message";
            Log log = new Log(LogType.Message, expectedString);

            StringAssert.Contains(log.GetMessageWithTimeStamp(), expectedString);
            StringAssert.Matches(log.GetMessageWithTimeStamp(), new System.Text.RegularExpressions.Regex(@"\[\d\d:\d\d:\d\d\]\s.*"));
        }
    }
}
