using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MessageHandler;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpChatTests
{
    [TestClass]
    public class MessagingHandlerTest
    {
        static Socket listener;
        static Socket client;
        static Socket acceptedClient;

        [ClassInitialize]
        public static void OneTimeSetup(TestContext context)
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 11000));
            listener.Listen(1);
        }

        [ClassCleanup]
        public static void OneTimeCleanup()
        {
            listener.Close(5);
            client.Close(5);
            acceptedClient.Close(5);
        }

        [TestInitialize]
        public void ClientSetup()
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(new IPEndPoint(IPAddress.Loopback, 11000));
            acceptedClient = listener.Accept();
        }

        [TestCleanup]
        public void ClientCleanup()
        {
            client.Close(5);
            acceptedClient.Close(5);
        }

        [TestMethod]
        public void MessagingHandlerSendsMessagesCorrectly()
        {
            Message sentMessage = Message.PublicMessage("test message");
            MessagingHandler.SendMessage(sentMessage, client);

            byte[] buffer = new byte[1024];
            int bytesReceived = acceptedClient.Receive(buffer);
            string messageString = Encoding.UTF8.GetString(buffer.ToArray(), 0, bytesReceived);
            Message receivedMessage = Message.Parse(messageString);

            Assert.AreEqual(sentMessage.messageHeader, receivedMessage.messageHeader);
            Assert.AreEqual(sentMessage.messageBody, receivedMessage.messageBody);
            Assert.AreEqual(sentMessage.GetNumberOfArguments(), receivedMessage.GetNumberOfArguments());
        }

        [TestMethod]
        public void MessagingHandlerReceivesMessagesCorrectly()
        {
            Message sentMessage = Message.PublicMessage("test message");
            MessagingHandler.SendMessage(sentMessage, client);

            Message receivedMessage = MessagingHandler.ReceiveMessage(acceptedClient);

            Assert.AreEqual(sentMessage.messageHeader, receivedMessage.messageHeader);
            Assert.AreEqual(sentMessage.messageBody, receivedMessage.messageBody);
            Assert.AreEqual(sentMessage.GetNumberOfArguments(), receivedMessage.GetNumberOfArguments());
        }

        [TestMethod]
        public async Task MessagingHandlerSendsMessagesAsyncCorrectly()
        {
            Message sentMessage = Message.PublicMessage("test message");
            await MessagingHandler.SendMessageAsync(sentMessage, client);

            byte[] buffer = new byte[1024];
            int bytesReceived = acceptedClient.Receive(buffer);
            string messageString = Encoding.UTF8.GetString(buffer.ToArray(), 0, bytesReceived);
            Message receivedMessage = Message.Parse(messageString);

            Assert.AreEqual(sentMessage.messageHeader, receivedMessage.messageHeader);
            Assert.AreEqual(sentMessage.messageBody, receivedMessage.messageBody);
            Assert.AreEqual(sentMessage.GetNumberOfArguments(), receivedMessage.GetNumberOfArguments());
        }

        [TestMethod]
        public async Task MessagingHandlerReceivesMessagesAsyncCorrectly()
        {
            Message sentMessage = Message.PublicMessage("test message");
            MessagingHandler.SendMessage(sentMessage, client);

            Message receivedMessage = await MessagingHandler.ReceiveMessageAsync(acceptedClient);

            Assert.AreEqual(sentMessage.messageHeader, receivedMessage.messageHeader);
            Assert.AreEqual(sentMessage.messageBody, receivedMessage.messageBody);
            Assert.AreEqual(sentMessage.GetNumberOfArguments(), receivedMessage.GetNumberOfArguments());
        }
    }
}
