namespace MyChat.Common.Tests
{
    using System.Net;
    using System.Net.Sockets;

    using NUnit.Framework;

    [TestFixture]
    public class NetworkTests
    {
        public static void TestTcpLisener()
        {
            var tcpListener = new TcpListener(IPAddress.Any, 3456);
            tcpListener.Start();
            tcpListener.AcceptTcpClientAsync();

            var tcpClient = new TcpClient();
            tcpClient.Connect(IPAddress.Parse("127.0.0.1"), 3456);

            var clnt = tcpListener.AcceptTcpClientAsync().ConfigureAwait(true).GetAwaiter().GetResult();
        }
    }
}