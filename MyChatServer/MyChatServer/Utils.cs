using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyChatServer
{
    using System.Net.Sockets;
    using System.Windows.Forms;

    public static class Utils
    {
        public static void defMsgBox(string msg)
        {
            MessageBox.Show(msg, "Chat Server");
        }

        public static System.Net.IPAddress TCPClient2IPAddress(TcpClient client)
        {            
            return (client.Client.RemoteEndPoint as System.Net.IPEndPoint).Address;
        }
    }
}
