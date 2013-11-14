namespace Andriy.MyChat.Server.Tests
{
    using System.Linq;
    using System.Net.Sockets;

    using NUnit.Framework;

    [TestFixture]
    public class UtilsTests
    {
        [Test]
        public void TCPClient2IPAddressTest()
        {
            const string HostName = "google.com";
            var tcpClient = new TcpClient(HostName, 80);
            var actual = Utils.TCPClient2IPAddress(tcpClient);
            tcpClient.Close();
            var expectedList = System.Net.Dns.GetHostEntry(HostName).AddressList;
            Assert.IsTrue(expectedList.Contains(actual));
        }
    }
}
