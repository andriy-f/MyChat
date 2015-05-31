namespace MyChat.IntegrationTests
{
    using System;
    using System.Configuration;
    using System.Threading;
    using System.Threading.Tasks;

    using Andriy.MyChat.Server;
    using Andriy.MyChat.Server.DAL;

    using Moq;

    using NUnit.Framework;

    using ChatClient = Andriy.MyChat.Client.ChatClient;

    [TestFixture]
    public class IntegrationTests
    {
        private int ServerPort { get; set; }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            this.ServerPort = int.Parse(ConfigurationManager.AppSettings[Consts.DefaultServerListeningPortProperty]);
        }

        [Test]
        public void OneServerOneClientTest()
        {
            const string Login = "testUser1";
            const string Pass = "testPass1";
            var serverDataContext = Mock.Of<IDataContext>(ctx => ctx.LoginExists(Login) &&
                ctx.ValidateLoginPass(Login, Pass));

            var server = new ChatServer(serverDataContext, this.ServerPort);

            var chatClient = new ChatClient();

            chatClient.init("localhost", this.ServerPort, Login, Pass);
            var authResult = chatClient.performAuth();
            Assert.AreEqual(0, authResult);

            var clientServerAgreementEstablished = chatClient.performAgreement();
            Assert.True(clientServerAgreementEstablished);

            var logonResult = chatClient.performLogonDef();
            Assert.AreEqual(0, logonResult);

            chatClient.startListener();

            var joinRoomResult = chatClient.performJoinRoom("r1", "testRoomPass");
            Assert.True(joinRoomResult);

            var messageReceivedBack = false;
            var syncRef = new object();
            chatClient.msgProcessor.addProcessor(
                "r1",
                (source, dest, msg) =>
                    {
                        if (msg == "testMsg")
                        {
                            lock (syncRef)
                            {
                                messageReceivedBack = true;
                            }
                        }
                    });

            chatClient.queueChatMsg(3, "r1", "testMsg");

            Func<bool> messageReceivedBackRead = () =>
                {
                    lock (syncRef)
                    {
                        return messageReceivedBack;
                    }
                };

            var waiter = Task.Run(
                () =>
                    {
                        while (!messageReceivedBackRead())
                        {
                            Thread.Sleep(100);
                        }
                    });
            Task.WaitAll(new[] { waiter }, 2000);
            Assert.True(messageReceivedBackRead());

            chatClient.stopListener();
            server.Finish();
        }
    }
}
