namespace MyChatServer
{
    using System.Collections.Generic;
    using System.Net.Sockets;

    using My.Cryptography;

    public class ChatClient
    {
        //public bool active = true; EQVIVALENT to client==null
        public List<string> rooms = new List<string>(3);//must be list of unique
        public TcpClient client=null;
        //bool freed = false;
        public AESCSPImpl cryptor;
    }
}