namespace Andriy.MyChat.Server.Tests
{
    using System.Net.Sockets;

    using NUnit.Framework;

    [TestFixture]
    public class UtilsTests
    {
        [Test]
        public void TCPClient2IPAddressTest()
        {
            var tcpClient = new TcpClient("google.com", 80);
            var actual = Utils.TCPClient2IPAddress(tcpClient);
            tcpClient.Close();
            Assert.AreEqual("173.194.39.71", actual.MapToIPv4().ToString());
        }
    }
}
