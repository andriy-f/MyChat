namespace Andriy.MyChat.Server
{
    using System.Net.Sockets;
    
    public static class Utils
    {
        public static System.Net.IPAddress TCPClient2IPAddress(TcpClient client)
        {            
            return (client.Client.RemoteEndPoint as System.Net.IPEndPoint).Address;
        }
    }
}
