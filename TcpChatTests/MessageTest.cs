using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MessageHandler;
using System.Linq;

namespace TcpChatTests
{
    [TestClass]
    public class MessageTest
    {
        [TestMethod]
        public void MessageShouldAddArgumentWithValue()
        {
            Message publicMessage = Message.PublicMessage("test message");

            publicMessage.AddArgument("testArg", false);

            Assert.IsTrue(publicMessage.HasArgument("testArg"));
            Assert.AreEqual("false", publicMessage.GetArgumentValue("testArg"), true);
        }

        [TestMethod]
        public void MessageShouldIgnoreCaseWhenGettingArgumentValue()
        {
            Message publicMessage = Message.PublicMessage("test message");

            publicMessage.AddArgument("testarg", false);

            Assert.AreEqual("false", publicMessage.GetArgumentValue("TESTARG"), true);
        }

        [TestMethod]
        public void MessageShouldReturnRightNumberOfArguments()
        {
            Message publicMessage = Message.PublicMessage("test message");

            publicMessage.AddArgument("arg1", false);
            publicMessage.AddArgument("arg2", false);

            Assert.AreEqual(2, publicMessage.GetNumberOfArguments());
        }

        [TestMethod]
        public void MessageShoudIgnoreCaseWhileCheckingForArgument()
        {
            Message publicMessage = Message.PublicMessage("test message");

            publicMessage.AddArgument("TESTARG", false);

            Assert.IsTrue(publicMessage.HasArgument("testarg"));
        }

        [TestMethod]
        public void MessageShoudReturnAllAddedArgumentsNames()
        {
            Message publicMessage = Message.PublicMessage("test message");

            publicMessage.AddArgument("argument1", false);
            publicMessage.AddArgument("argument2", true);
            publicMessage.AddArgument("argument3", false);

            string[] actualArgs = publicMessage.GetArgumentsNames();
            string[] expectedArgs = { "argument1", "argument2", "argument3" };

            Assert.AreEqual(expectedArgs[0], actualArgs[0]);
            Assert.AreEqual(expectedArgs[1], actualArgs[1]);
            Assert.AreEqual(expectedArgs[2], actualArgs[2]);
        }

        [TestMethod]
        public void MessageShoudReturnRightStringRepresentation()
        {
            Message publicMessage = Message.PublicMessage("test message");

            publicMessage.AddArgument("arg1", "test");
            publicMessage.AddArgument("arg2", true);
            publicMessage.AddArgument("arg3", 123);

            string actualString = publicMessage.ToString();
            string expectedString = "Public_Message arg1=test,arg2=True,arg3=123\ntest message";

            Assert.AreEqual(expectedString, actualString);
        }

        [TestMethod]
        public void MessageShoudReturnWellParsedMessageObject()
        {
            string messageString = "Public_Message arg1=test,arg2=True,arg3=123\ntest message";

            Message publicMessage = Message.Parse(messageString);

            Assert.AreEqual(MessageHeader.Public_Message, publicMessage.messageHeader);

            Assert.AreEqual(3, publicMessage.GetNumberOfArguments());

            Assert.IsTrue(publicMessage.HasArgument("arg1"), string.Join("; ", publicMessage.GetArgumentsNames()));
            Assert.IsTrue(publicMessage.HasArgument("arg2"));
            Assert.IsTrue(publicMessage.HasArgument("arg3"));

            Assert.AreEqual("test", publicMessage.GetArgumentValue("arg1"));
            Assert.AreEqual("True", publicMessage.GetArgumentValue("arg2"));
            Assert.AreEqual("123", publicMessage.GetArgumentValue("arg3"));

            Assert.AreEqual("test message", publicMessage.messageBody);
        }
    }
}
