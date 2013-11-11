namespace Andriy.MyChat.Server
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using Andriy.MyChat.Server.DAL;
    using Andriy.Security.Cryptography;

    using log4net;

    public class ChatServer
    {
        #region Fields

        private const int AgreementLength = 32;

        ////public static System.Collections.Hashtable loginBase = new System.Collections.Hashtable(3);//login/pass
        private static readonly ILog Log = LogManager.GetLogger(typeof(ChatServer));

        internal static readonly byte[] CryptoIv1 = { 111, 62, 131, 223, 199, 122, 219, 32, 13, 147, 249, 67, 137, 161, 97, 104 };

        private readonly Dictionary<string, ChatClient> clients = new Dictionary<string, ChatClient>(10); // login/ChatClient

        private readonly Dictionary<string, RoomParams> roomBase = new Dictionary<string, RoomParams>(); // room/RoomParams
        
        private readonly List<string> unusedClients = new List<string>(5);

        private readonly List<string> unusedRooms = new List<string>(5);

        private Thread listenerThread;

        private DataContext dataContext;

        private bool continueToListen = true;
        
        //// static ECDSAWrapper seanceDsaClientChecker;//checks client's messages
        
        //// static ECDSAWrapper seanceDsaServerSigner;//signs servers messages
        
        public int Port { get; private set; }

        #endregion

        #region Init&Free

        public static void FreeTCPClient(TcpClient client)
        {
            if (client != null)
            {
                if (client.Connected)
                {
                    client.GetStream().Close();
                }

                client.Close();
            }
        }

        public void Init(DataContext newDataContext, int port)
        {
            this.dataContext = newDataContext;

            this.Port = port;
            
            this.clients.Clear();

            // Listener thread
            if (this.listenerThread != null)
            {
                this.listenerThread.Abort();
            }

            this.listenerThread = new Thread(this.Listen)
                             {
                                 Priority = ThreadPriority.Lowest
                             };
            this.listenerThread.Start();
            Log.Info("Listening started");
        }

        public void Finish()
        {
            if (this.listenerThread != null)
            {
                this.continueToListen = false;
                this.listenerThread.Join();
            }
        }

        #endregion

        #region Listen

        public void Listen()
        {
            TcpListener tcpListener = null;
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, this.Port);
                tcpListener.Start();
                
                while (this.continueToListen)
                {
                    if (tcpListener.Pending())
                    {
                        this.ProcessNewConnection(tcpListener.AcceptTcpClient());
                    }
                    else
                    {
                        // Processing current connections
                        foreach (var de in this.clients)
                        {
                            de.Value.Login = de.Key; // TODO: refactor
                            this.ProcessCurrentConnection(de.Value);
                        }

                        // free resources from logout of clients
                        foreach (var flogin in this.unusedClients)
                        {
                            this.RemoveClient(flogin);
                        }

                        this.unusedClients.Clear();

                        // Free unocupied rooms - deprecated because of saving room params (password)
                        // cleanupRooms();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                if (tcpListener != null)
                {
                    tcpListener.Stop();
                }

                Log.Info("Listening finished");
            }
        }

        private void ProcessNewConnection(TcpClient tcp)
        {
            var clientIPAddress = Utils.TCPClient2IPAddress(tcp);
            Log.DebugFormat("Connected from {0}", clientIPAddress);
            var clientStream = tcp.GetStream();
            clientStream.ReadTimeout = 1000;

            try
            {
                var chatClient = new ChatClient(tcp);
                int authatt = chatClient.Verify();
                switch (authatt)
                {
                    case 0:
                        if (chatClient.SetUpSecureChannel() == 0)
                        {
                            var type = (byte)clientStream.ReadByte();
                            string login, pass;
                            byte[] bytes;
                            switch (type)
                            {
                                case 0:

                                    // Logon attempt
                                    bytes = chatClient.ReadWrappedEncMsg();
                                    ParseLogonMsg(bytes, out login, out pass);
                                    if (this.dataContext.ValidateLoginPass(login, pass))
                                    {
                                        if (this.IsLogged(login))
                                        {
                                            var oldUserParams = this.clients[login];
                                            int oldresp = -2;
                                            if (oldUserParams.Tcp.Connected)
                                            {
                                                var oldStream = oldUserParams.Tcp.GetStream();

                                                try
                                                {
                                                    oldStream.WriteByte(10);
                                                    oldresp = oldStream.ReadByte();
                                                }
                                                catch (IOException)
                                                {
                                                    // Timeout - old client probably dead
                                                }
                                            }

                                            if (oldresp == 10)
                                            {
                                                // Client with login <login> still alive -> new login attempt invalid
                                                clientStream.WriteByte(1);
                                                FreeTCPClient(tcp);
                                                Log.DebugFormat(
                                                        "Logon from IP '{0}' failed: User '{1}' already logged on", 
                                                        clientIPAddress, 
                                                        login);
                                            }
                                            else
                                            {
                                                // old client with login <login> dead -> dispose of him and connect new
                                                FreeTCPClient(oldUserParams.Tcp);
                                                this.RemoveClient(login);
                                                this.ProcessAndAcceptNewClient(tcp, login, chatClient.Cryptor);
                                                Log.DebugFormat(
                                                        "Logon from IP '{0}' success: User '{1}' from IP  logged on (old client disposed)", 
                                                        clientIPAddress, 
                                                        login);
                                            }
                                        }
                                        else
                                        {
                                            this.ProcessAndAcceptNewClient(tcp, login, chatClient.Cryptor);
                                            Log.DebugFormat(
                                                    "Logon from IP '{0}' success: User '{1}' from IP  logged on", 
                                                    clientIPAddress, 
                                                    login);
                                        }
                                    }
                                    else
                                    {
                                        clientStream.WriteByte(2);
                                        FreeTCPClient(tcp);
                                        Log.DebugFormat(
                                                "Logon from IP '{0}' failed: Login '{1}'//Password not recognized", 
                                                clientIPAddress, 
                                                login);
                                    }

                                    break;
                                case 1:

                                    // Registration without logon
                                    bytes = ReadWrappedEncMsg(clientStream, chatClient.Cryptor);
                                    ParseLogonMsg(bytes, out login, out pass);
                                    if (!this.dataContext.LoginExists(login))
                                    {
                                        this.dataContext.AddUser(login, pass);
                                        clientStream.WriteByte(0);
                                        Log.DebugFormat("Registration success: User '{0}' registered", login);
                                    }
                                    else
                                    {
                                        clientStream.WriteByte(1);
                                        Log.DebugFormat("Registration failed: User '{0}' already registered", login);
                                    }

                                    FreeTCPClient(tcp);
                                    break;
                                default:

                                    // Wrong data received
                                    throw new Exception();
                            }
                        }

                        break;
                    case 1:
                        FreeTCPClient(tcp);
                        Log.DebugFormat("Auth from IP '{0}' fail because client is not legit", clientIPAddress);
                        
                        // TODO: Ban IP if too many attempts...
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(new Exception(string.Format("New connetion from IP {0} failed", clientIPAddress), ex));
                FreeTCPClient(tcp);

                // Ban IP ipAddress...
            }
        }

        #endregion

        #region Processors
        
        private void ProcessAndAcceptNewClient(TcpClient client, string login, AESCSPImpl cryptor1)
        {
            var chatClient = new ChatClient(client);
            chatClient.Cryptor = cryptor1;
            this.clients.Add(login, chatClient);
            client.GetStream().WriteByte(0);
        }

        //// <summary>
        //// Processes authentification attempt from new client
        //// </summary>
        //// <param name="stream"></param>
        //// <returns>0 if ok, 1 if wrong, 2 if exception</returns>
        ////internal static int ProcessAuth(NetworkStream stream)
        ////{
        ////    try
        ////    {
        ////        // Check if client is legit
        ////        byte[] send = Randoms.genSecureRandomBytes(100);
        ////        WriteWrappedMsg(stream, send);
        ////        byte[] rec = ReadWrappedMsg(stream);

        ////        // Program.LogEvent(HexRep.ToString(rec));
        ////        bool clientLegit = Crypto.Utils.ClientVerifier.verifyHash(send, rec);
        ////        if (clientLegit)
        ////        {
        ////            // Clients want to know if server is legit
        ////            rec = ReadWrappedMsg(stream);
        ////            send = Crypto.Utils.ServerSigner.signHash(rec);
        ////            WriteWrappedMsg(stream, send);
        ////            return 0;
        ////        }
        ////        else
        ////        {
        ////            return 1;
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        Program.LogEvent(string.Format("Error while authentificating: {0}{1}", Environment.NewLine, ex));
        ////        return 2;
        ////    }
        ////}
        
        private void ProcessCurrentConnection(ChatClient chatClient)
        {
            TcpClient client = chatClient.Tcp;
            if (client != null && client.Connected)
            {
                string clientLogin = chatClient.Login;
                var stream = client.GetStream();
                if (stream.DataAvailable)
                {
                    try
                    {
                        // Parsing data from client
                        byte[] data;
                        int type = stream.ReadByte();
                        string source, dest, messg;
                        switch (type)
                        {
                            case 3: // Message to room
                                data = ReadWrappedEncMsg(stream, chatClient.Cryptor);
                                ParseChatMsg(data, out source, out dest, out messg); // dest - room
                                Log.DebugFormat("<Room>[{0}]->[{1}]: \"{2}\"", source, dest, messg);

                                // if user(source) in room(dest)
                                var senderClient = this.clients[source];
                                if (senderClient.Rooms.Contains(dest))
                                {
                                    var roomParams = this.roomBase[dest];
                                    foreach (string roomUsr in roomParams.Users)
                                    {
                                        var destinationClient = this.clients[roomUsr];
                                        if (destinationClient.Tcp.Connected)
                                        {
                                            try
                                            {
                                                var destStream = destinationClient.Tcp.GetStream();
                                                destStream.WriteByte(3);
                                                WriteWrappedEncMsg(destStream, data, destinationClient.Cryptor);
                                            }
                                            catch (IOException)
                                            {
                                                this.unusedClients.Add(roomUsr);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // client not in the room he marked as dest
                                    stream.WriteByte(1);
                                }

                                break;
                            case 4: // Message to user
                                data = ReadWrappedEncMsg(stream, chatClient.Cryptor);
                                ParseChatMsg(data, out source, out dest, out messg); // dest - user
                                Log.DebugFormat("<User>[{0}]->[{1}]: \"{2}\"", source, dest, messg);
                                if (this.clients.ContainsKey(dest))
                                {
                                    var destinationClient = this.clients[dest];
                                    if (destinationClient.Tcp.Connected)
                                    {
                                        try
                                        {
                                            var destStream = destinationClient.Tcp.GetStream();
                                            destStream.WriteByte(4);
                                            WriteWrappedEncMsg(destStream, data, destinationClient.Cryptor);
                                        }
                                        catch (IOException)
                                        {
                                            this.unusedClients.Add(dest);
                                        }
                                    }
                                }
                                else
                                {
                                    // no Success - No Such Dest
                                    stream.WriteByte(1);
                                }

                                break;
                            case 5: // Message to All
                                data = ReadWrappedEncMsg(stream, chatClient.Cryptor);

                                // Display to all
                                ParseChatMsg(data, out source, out dest, out messg);
                                Log.DebugFormat("<All>[{0}]->[{1}]: \"{2}\"", source, dest, messg);
                                foreach (var destDe in this.clients)
                                {
                                    var destinationClient = destDe.Value;
                                    if (destinationClient.Tcp.Connected)
                                    {
                                        try
                                        {
                                            var destStream = destinationClient.Tcp.GetStream();
                                            destStream.WriteByte(5);
                                            WriteWrappedEncMsg(destStream, data, destinationClient.Cryptor);
                                        }
                                        catch (IOException)
                                        {
                                            this.unusedClients.Add(destDe.Key);
                                        }
                                    }
                                }

                                break;
                            case 6: // Join Room
                                string room, pass;
                                data = ReadWrappedEncMsg(stream, chatClient.Cryptor);
                                ParseJoinRoomMsg(data, out room, out pass);
                                if (this.RoomExist(room))
                                {
                                    if (this.ConfirmRoomPass(room, pass))
                                    {
                                        // Allow join 
                                        this.AddUserToRoom(room, clientLogin);
                                        stream.WriteByte(0); // Success
                                        Log.DebugFormat(
                                                "User '{0}' joined room '{1}' with pass '{2}'", 
                                                clientLogin, 
                                                room, 
                                                pass);
                                    }
                                    else
                                    {
                                        stream.WriteByte(1); // Room Exist, invalid pass
                                        Log.DebugFormat(
                                                "User '{0}' failed to join room '{1}' because invalid pass '{2}'", 
                                                clientLogin, 
                                                room, 
                                                pass);
                                    }
                                }
                                else
                                {
                                    // Room doesn't exist
                                    this.AddRoom(room, pass);
                                    this.AddUserToRoom(room, clientLogin);
                                    stream.WriteByte(0); // Success
                                    Log.DebugFormat(
                                            "User '{0}' joined new room '{1}' with pass '{2}'", 
                                            clientLogin, 
                                            room, 
                                            pass);
                                }

                                break;
                            case 7: // Logout //user - de.Key, room.users - de.Key, if room empty -> delete
                                stream.WriteByte(0); // approve - need?

                                // Free Resources
                                this.unusedClients.Add(clientLogin);
                                FreeTCPClient(client);
                                Log.DebugFormat("Client '{0}' performed Logout", clientLogin);
                                break;
                            case 8: // Get Rooms                                        
                                data = FormatGetRoomsMsgReply(this.roomBase.Keys);
                                stream.WriteByte(8);
                                WriteWrappedMsg(stream, data);
                                Log.DebugFormat("Client '{0}' requested rooms", clientLogin);
                                break;
                            case 9: // Leave room
                                data = ReadWrappedMsg(stream);
                                string leaveroom = ParseLeaveRoomMsg(data);
                                this.RemoveClientFromRoom(clientLogin, leaveroom);
                                stream.WriteByte(0); // approve - need?
                                Log.DebugFormat("Client '{0}' leaved room '{1}'", clientLogin, leaveroom);
                                break;
                            case 11: // Get Room users
                                string roomname = System.Text.Encoding.UTF8.GetString(ReadWrappedMsg(stream));
                                data = this.FormatRoomUsers(roomname);
                                stream.WriteByte(11);
                                WriteWrappedMsg(stream, data);
                                Log.DebugFormat("Client '{0}' requested room users", clientLogin);
                                break;
                            default: // Invalid message from client
                                throw new Exception("Client send unknown token");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Invalid data from current client
                        Log.DebugFormat(
                                "Received invalid data from client with login '{1}', IP '{2}'-> kick.{0}Reason:{0}{3}", 
                                Environment.NewLine, 
                                clientLogin, 
                                Utils.TCPClient2IPAddress(client), 
                                ex);
                        this.unusedClients.Add(clientLogin);
                        FreeTCPClient(client);
                    }
                }
            }
        }

        #endregion

        #region Rooms&Clients in local datastore

        private bool IsLogged(string login)
        {
            return this.clients.ContainsKey(login);
        }

        private bool AddRoom(string room, string pass)
        {
            if (this.roomBase.ContainsKey(room))
            {
                return false;
            }

            var newrp = new RoomParams { Password = pass };
            this.roomBase.Add(room, newrp);
            return true;
        }

        private void AddUserToRoom(string room, string login)
        {
            this.clients[login].Rooms.Add(room);
            this.roomBase[room].Users.Add(login);
        }

        private void RemoveClient(string login)
        {
            // Removes client from cliBase and every room, if room empty -> free it
            this.clients.Remove(login);
            foreach (var de in this.roomBase)
            {
                var rp = de.Value;
                rp.Users.Remove(login);
                if (rp.Users.Count == 0)
                {
                    this.unusedRooms.Add(de.Key);
                }
            }

            foreach (var froom in this.unusedRooms)
            {
                this.roomBase.Remove(froom);
            }

            this.unusedRooms.Clear();
        }

        private void RemoveClientFromRoom(string login, string room)
        {
            this.clients[login].Rooms.Remove(room);
            this.roomBase[room].Users.Remove(login);
        }

        // static void cleanupRooms()
        // {
        // foreach (System.Collections.DictionaryEntry de in roomBase)
        // {
        // RoomParams rp = ((RoomParams)de.Value);
        // if (rp.users.Count == 0) roomsToFree.Add((string)de.Key);
        // }
        // foreach (string froom in roomsToFree)
        // roomBase.Remove(froom);
        // roomsToFree.Clear();
        // }
        private bool RoomExist(string room)
        {
            return this.roomBase.ContainsKey(room);
        }

        private bool ConfirmRoomPass(string room, string pass)
        {
            return this.roomBase[room].Password == pass;
        }

        #endregion

        #region Formatting Messages

        ////private static byte[] FormatChatMsg(byte msgtype, string source, string dest, string message)
        ////{
        ////    // dest sometimes is "all"
        ////    // int headerSize = 13;
        ////    byte[] sourceB = System.Text.Encoding.UTF8.GetBytes(source);
        ////    byte[] destB = System.Text.Encoding.UTF8.GetBytes(dest);
        ////    byte[] messageB = System.Text.Encoding.UTF8.GetBytes(message);
        ////    int dataSize = sourceB.Length + destB.Length + messageB.Length;

        ////    // Header - 1+4+4+4         
        ////    byte[] msg = new byte[13 + dataSize];
        ////    msg[0] = msgtype;
        ////    BitConverter.GetBytes(sourceB.Length).CopyTo(msg, 1);
        ////    BitConverter.GetBytes(destB.Length).CopyTo(msg, 5);
        ////    BitConverter.GetBytes(messageB.Length).CopyTo(msg, 9);

        ////    // data
        ////    sourceB.CopyTo(msg, 13);
        ////    destB.CopyTo(msg, 13 + sourceB.Length);
        ////    messageB.CopyTo(msg, 13 + sourceB.Length + destB.Length);
        ////    return msg;
        ////}

        ////private static byte[] FormatGetRoomsMsgReply(string[] rooms)
        ////{
        ////    int i, n = rooms.Length;
        ////    byte[][] roomB = new byte[n][];
        ////    int msgLen = 1 + 4; // type+roomCount
        ////    for (i = 0; i < n; i++)
        ////    {
        ////        roomB[i] = System.Text.Encoding.UTF8.GetBytes(rooms[i]);
        ////        msgLen += 4 + roomB.Length;
        ////    }

        ////    // Formatting Message
        ////    byte[] data = new byte[msgLen];
        ////    data[0] = 0; // type
        ////    byte[] roomCntB = BitConverter.GetBytes(n);
        ////    roomCntB.CopyTo(data, 1);
        ////    int pos = 5;
        ////    byte[] roomBSize;
        ////    for (i = 0; i < n; i++)
        ////    {
        ////        roomBSize = BitConverter.GetBytes(roomB.Length);
        ////        roomBSize.CopyTo(data, pos);
        ////        pos += 4;
        ////        roomB[i].CopyTo(data, pos);
        ////        pos += roomB[i].Length;
        ////    }

        ////    return data;
        ////}

        private static byte[] FormatGetRoomsMsgReply(ICollection rooms)
        {
            int i, n = rooms.Count;
            IEnumerator enmr = rooms.GetEnumerator();
            enmr.Reset();
            var roomB = new byte[n][];
            int msgLen = 1 + 4; // type+roomCount
            for (i = 0; i < n; i++)
            {
                enmr.MoveNext();
                string s = (string)enmr.Current;
                roomB[i] = System.Text.Encoding.UTF8.GetBytes(s);
                msgLen += 4 + roomB[i].Length;
            }

            // Formatting Message
            byte[] data = new byte[msgLen];
            data[0] = 0; // type
            byte[] roomCntB = BitConverter.GetBytes(n);
            roomCntB.CopyTo(data, 1);
            int pos = 5;
            byte[] roomBiSize;
            for (i = 0; i < n; i++)
            {
                roomBiSize = BitConverter.GetBytes(roomB[i].Length); // 4 bytes
                roomBiSize.CopyTo(data, pos);
                pos += 4;
                roomB[i].CopyTo(data, pos);
                pos += roomB[i].Length;
            }

            return data;
        }

        private byte[] FormatRoomUsers(string room)
        {
            var roomParams = this.roomBase[room];

            // foreach (string roomUsr in roomParams.users)
            var users = roomParams.Users;

            int i, n = users.Count;

            var usrB = new byte[n][];
            int msgLen = 1 + 4; // type+roomCount
            for (i = 0; i < n; i++)
            {
                usrB[i] = System.Text.Encoding.UTF8.GetBytes(users[i]);
                msgLen += 4 + usrB[i].Length;
            }

            // Formatting Message
            var data = new byte[msgLen];
            data[0] = 0; // type
            var usrCntB = BitConverter.GetBytes(n);
            usrCntB.CopyTo(data, 1);
            int pos = 5;
            for (i = 0; i < n; i++)
            {
                BitConverter.GetBytes(usrB[i].Length).CopyTo(data, pos);

                // 4 bytes                
                pos += 4;
                usrB[i].CopyTo(data, pos);
                pos += usrB[i].Length;
            }

            return data;
        }

        #endregion

        #region Parsing

        private static void ParseLogonMsg(byte[] bytes, out string login, out string pass)
        {
            // Read header = type+loginSize+passSize (9 bytes)
            int hsz = 9;

            // bytes[0] must be 0
            int loginSize = BitConverter.ToInt32(bytes, 1);
            int passSize = BitConverter.ToInt32(bytes, 5);

            // Read data
            login = System.Text.Encoding.UTF8.GetString(bytes, hsz, loginSize);
            pass = System.Text.Encoding.UTF8.GetString(bytes, hsz + loginSize, passSize);
        }

        private static void ParseChatMsg(byte[] bytes, out string source, out string dest, out string message)
        {
            // bytes[0] must be 3 or 4 or 5
            // header Size =13
            // Reading header
            int sourceBSize = BitConverter.ToInt32(bytes, 1);
            int destBSize = BitConverter.ToInt32(bytes, 5);
            int messageBSize = BitConverter.ToInt32(bytes, 9);

            // Reading data                      
            source = System.Text.Encoding.UTF8.GetString(bytes, 13, sourceBSize);
            dest = System.Text.Encoding.UTF8.GetString(bytes, 13 + sourceBSize, destBSize);
            message = System.Text.Encoding.UTF8.GetString(bytes, 13 + sourceBSize + destBSize, messageBSize);
        }

        private static void ParseJoinRoomMsg(byte[] bytes, out string room, out string pass)
        {
            // bytes[0] must be 6
            // header Size = 9
            // Reading header
            int roomBlen = BitConverter.ToInt32(bytes, 1);
            int passBlen = BitConverter.ToInt32(bytes, 5);

            // Reading data                      
            room = System.Text.Encoding.UTF8.GetString(bytes, 9, roomBlen);
            pass = System.Text.Encoding.UTF8.GetString(bytes, 9 + roomBlen, passBlen);
        }

        private static string ParseLeaveRoomMsg(byte[] bytes)
        {
            int roomBsize = BitConverter.ToInt32(bytes, 0); // reads first 4 bytes - header
            return System.Text.Encoding.UTF8.GetString(bytes, 4, roomBsize);
        }

        #endregion

        #region ReadWrite wrapUnwrap

        private static int ReadInt32(NetworkStream stream)
        {
            var data = new byte[4];
            stream.Read(data, 0, data.Length);
            return BitConverter.ToInt32(data, 0);
        }

        public static byte[] ReadWrappedMsg(NetworkStream stream)
        {
            int streamDataSize = ReadInt32(stream);
            var streamData = new byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return streamData;
        }

        ////private static int ReadWrappedMsg2(NetworkStream stream, ref byte[] read)
        ////{
        ////    int streamDataSize = ReadInt32(stream);
        ////    if (streamDataSize >= read.Length)
        ////    {
        ////        int readSZ = stream.Read(read, 0, streamDataSize);
        ////        return readSZ;
        ////    }
        ////    else
        ////    {
        ////        throw new ArgumentException("Too small to read incoming data", "read");
        ////    }
        ////}

        private static byte[] ReadWrappedEncMsg(NetworkStream stream, AESCSPImpl cryptor)
        {
            int streamDataSize = ReadInt32(stream);
            var streamData = new byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return cryptor.Decrypt(streamData);
        }

        public static void WriteWrappedMsg(Stream stream, byte[] bytes)
        {
            var data = new byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }

        private static void WriteWrappedEncMsg(Stream stream, byte[] plain, AESCSPImpl cryptor)
        {
            var bytes = cryptor.Encrypt(plain);
            var data = new byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }

        #endregion
    }
}