namespace MyChatServer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    using CustomCrypto;

    using My.Cryptography;

    using MyChatServer.DAL;

    public class ChatServer
    {
        #region Fields

        private const int AgreementLength = 32;

        ////public static System.Collections.Hashtable loginBase = new System.Collections.Hashtable(3);//login/pass

        private static readonly Dictionary<string, ChatClient> ClientBase = new Dictionary<string, ChatClient>(10); // login/ChatClient

        private static readonly Hashtable RoomBase = new Hashtable(3); // room/RoomParams
        
        private static readonly byte[] Iv1 = { 111, 62, 131, 223, 199, 122, 219, 32, 13, 147, 249, 67, 137, 161, 97, 104 };

        private static readonly MyRandoms Randoms = new MyRandoms();

        private static readonly List<string> ClientsToFree = new List<string>(5);

        private static readonly List<string> RoomsToFree = new List<string>(5);

        private static Thread listenerThread;

        private static DataGetter dataGetter;

        private static bool continueToListen = true;

        //// static ECDSAWrapper seanceDsaClientChecker;//checks client's messages
        
        //// static ECDSAWrapper seanceDsaServerSigner;//signs servers messages
        
        #endregion

        #region Init&Free

        public static void init()
        {
            dataGetter = DataGetter.Instance;
            
            ClientBase.Clear();

            // Listener thread
            if (listenerThread != null)
            {
                listenerThread.Abort();
            }

            listenerThread = new Thread(Listen)
                             {
                                 Priority = ThreadPriority.Lowest
                             };
            listenerThread.Start();
            Program.LogEvent("Listening started");
        }

        public static void Finish()
        {
            if (listenerThread != null)
            {
                continueToListen = false;
                listenerThread.Join();
            }
        }

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

        #endregion

        #region Listen

        public static void Listen()
        {
            TcpListener tcpListener = null;
            try
            {
                int port = Properties.Settings.Default.Port;
                var localAddr = IPAddress.Any; // System.Net.IPAddress.Parse("127.0.0.1");
                tcpListener = new TcpListener(localAddr, port);
                tcpListener.Start();
                
                while (continueToListen)
                {
                    if (tcpListener.Pending())
                    {
                        ProcessPendingConnection(tcpListener.AcceptTcpClient());
                    }
                    else
                    {
                        // Processing current connections
                        foreach (var de in ClientBase)
                        {
                            de.Value.Login = de.Key; // TODO: refactor
                            ProcessCurrentConnection(de.Value);
                        }

                        // free resources from logout of clients
                        foreach (var flogin in ClientsToFree)
                        {
                            removeClient(flogin);
                        }

                        ClientsToFree.Clear();

                        // Free unocupied rooms - deprecated because of saving room params (password)
                        // cleanupRooms();
                    }
                }
            }
            catch (Exception e)
            {
                Program.LogException(e);
            }
            finally
            {
                if (tcpListener != null)
                {
                    tcpListener.Stop();
                }

                Program.LogEvent("Listening finished");
            }
        }

        private static void ProcessPendingConnection(TcpClient client)
        {
            var clientIPAddress = Utils.TCPClient2IPAddress(client);
            Program.LogEvent(string.Format("Connected from {0}", clientIPAddress));
            var stream = client.GetStream();
            stream.ReadTimeout = 1000;

            try
            {
                var chatClient = new ChatClient(client);
                int authatt = chatClient.Verify();
                switch (authatt)
                {
                    case 0:
                        AESCSPImpl cryptor;
                        if (ProcessAgreement(stream, out cryptor) == 0)
                        {
                            var type = (byte)stream.ReadByte();
                            string login, pass;
                            byte[] bytes;
                            switch (type)
                            {
                                case 0:

                                    // Logon attempt
                                    bytes = ReadWrappedEncMsg(stream, cryptor);
                                    ParseLogonMsg(bytes, out login, out pass);
                                    if (dataGetter.ValidateLoginPass(login, pass))
                                    {
                                        if (IsLogged(login))
                                        {
                                            var oldUserParams = ClientBase[login];
                                            int oldresp = -2;
                                            if (oldUserParams.Tcp.Connected)
                                            {
                                                NetworkStream oldStream = oldUserParams.Tcp.GetStream();

                                                try
                                                {
                                                    oldStream.WriteByte(10);
                                                    oldresp = oldStream.ReadByte();
                                                }
                                                catch (System.IO.IOException)
                                                {
                                                    // Timeout - old client probably dead
                                                }
                                            }

                                            if (oldresp == 10)
                                            {
                                                // Client with login <login> still alive -> new login attempt invalid
                                                stream.WriteByte(1);
                                                FreeTCPClient(client);
                                                Program.LogEvent(
                                                    string.Format(
                                                        "Logon from IP '{0}' failed: User '{1}' already logged on", 
                                                        clientIPAddress, 
                                                        login));
                                            }
                                            else
                                            {
                                                // old client with login <login> dead -> dispose of him and connect new
                                                FreeTCPClient(oldUserParams.Tcp);
                                                removeClient(login);
                                                ProcessAndAcceptNewClient(client, login, cryptor);
                                                Program.LogEvent(
                                                    string.Format(
                                                        "Logon from IP '{0}' success: User '{1}' from IP  logged on (old client disposed)", 
                                                        clientIPAddress, 
                                                        login));
                                            }
                                        }
                                        else
                                        {
                                            ProcessAndAcceptNewClient(client, login, cryptor);
                                            Program.LogEvent(
                                                string.Format(
                                                    "Logon from IP '{0}' success: User '{1}' from IP  logged on", 
                                                    clientIPAddress, 
                                                    login));
                                        }
                                    }
                                    else
                                    {
                                        stream.WriteByte(2);
                                        FreeTCPClient(client);
                                        Program.LogEvent(
                                            string.Format(
                                                "Logon from IP '{0}' failed: Login '{1}'//Password not recognized", 
                                                clientIPAddress, 
                                                login));
                                    }

                                    break;
                                case 1:

                                    // Registration without logon
                                    bytes = ReadWrappedEncMsg(stream, cryptor);
                                    ParseLogonMsg(bytes, out login, out pass);
                                    if (!dataGetter.ValidateLogin(login))
                                    {
                                        dataGetter.AddNewLoginPass(login, pass);
                                        stream.WriteByte(0);
                                        Program.LogEvent(
                                            string.Format("Registration success: User '{0}' registered", login));
                                    }
                                    else
                                    {
                                        stream.WriteByte(1);
                                        Program.LogEvent(
                                            string.Format("Registration failed: User '{0}' already registered", login));
                                    }

                                    FreeTCPClient(client);
                                    break;
                                default:

                                    // Wrong data received
                                    throw new Exception();
                            }
                        }

                        break;
                    case 1:
                        FreeTCPClient(client);
                        Program.LogEvent(string.Format("Auth from IP '{0}' fail because client is not legit", clientIPAddress));
                        
                        // TODO: Ban IP if too many attempts...
                        break;
                }
            }
            catch (Exception ex)
            {
                Program.LogException(new Exception(string.Format("New connetion from IP {0} failed", clientIPAddress), ex));
                FreeTCPClient(client);

                // Ban IP ipAddress...
            }
        }

        #endregion

        #region Processors

        internal static void ProcessAndAcceptNewClient(TcpClient client, string login, AESCSPImpl cryptor1)
        {
            var chatClient = new ChatClient(client);
            chatClient.Cryptor = cryptor1;
            ClientBase.Add(login, chatClient);
            client.GetStream().WriteByte(0);
        }

        /// <summary>
        /// Processes authentification attempt from new client
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>0 if ok, 1 if wrong, 2 if exception</returns>
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

        internal static int ProcessAgreement(NetworkStream stream, out AESCSPImpl cryptor)
        {
            try
            {
                var ecdh1 = new ECDHWrapper(AgreementLength);
                byte[] recCliPub = ReadWrappedMsg(stream);
                WriteWrappedMsg(stream, ecdh1.PubData);
                byte[] agr = ecdh1.calcAgreement(recCliPub);

                const int AESKeyLength = 32;
                var aeskey = new byte[AESKeyLength];
                Array.Copy(agr, 0, aeskey, 0, AESKeyLength);

                cryptor = new AESCSPImpl(aeskey, Iv1);
                return 0;
            }
            catch (Exception ex)
            {
                Program.LogEvent(string.Format("Error while completing agreement: {0}{1}", Environment.NewLine, ex));
                cryptor = null;
                return 1;
            }
        }

        private static void ProcessCurrentConnection(ChatClient chatClient)
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
                                Program.LogEvent(string.Format("<Room>[{0}]->[{1}]: \"{2}\"", source, dest, messg));

                                // if user(source) in room(dest)
                                var senderClient = ClientBase[source];
                                if (senderClient.Rooms.Contains(dest))
                                {
                                    var roomParams = (RoomParams)RoomBase[dest];
                                    foreach (string roomUsr in roomParams.Users)
                                    {
                                        var destinationClient = ClientBase[roomUsr];
                                        if (destinationClient.Tcp.Connected)
                                        {
                                            try
                                            {
                                                var destStream = destinationClient.Tcp.GetStream();
                                                destStream.WriteByte(3);
                                                WriteWrappedEncMsg(destStream, data, destinationClient.Cryptor);
                                            }
                                            catch (System.IO.IOException)
                                            {
                                                ClientsToFree.Add(roomUsr);
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
                                Program.LogEvent(string.Format("<User>[{0}]->[{1}]: \"{2}\"", source, dest, messg));
                                if (ClientBase.ContainsKey(dest))
                                {
                                    var destinationClient = ClientBase[dest];
                                    if (destinationClient.Tcp.Connected)
                                    {
                                        try
                                        {
                                            var destStream = destinationClient.Tcp.GetStream();
                                            destStream.WriteByte(4);
                                            WriteWrappedEncMsg(destStream, data, destinationClient.Cryptor);
                                        }
                                        catch (System.IO.IOException)
                                        {
                                            ClientsToFree.Add(dest);
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
                                Program.LogEvent(string.Format("<All>[{0}]->[{1}]: \"{2}\"", source, dest, messg));
                                foreach (var destDE in ClientBase)
                                {
                                    var destinationClient = destDE.Value;
                                    if (destinationClient.Tcp.Connected)
                                    {
                                        try
                                        {
                                            var destStream = destinationClient.Tcp.GetStream();
                                            destStream.WriteByte(5);
                                            WriteWrappedEncMsg(destStream, data, destinationClient.Cryptor);
                                        }
                                        catch (System.IO.IOException)
                                        {
                                            ClientsToFree.Add(destDE.Key);
                                        }
                                    }
                                }

                                break;
                            case 6: // Join Room
                                string room, pass;
                                data = ReadWrappedEncMsg(stream, chatClient.Cryptor);
                                ParseJoinRoomMsg(data, out room, out pass);
                                if (RoomExist(room))
                                {
                                    if (confirmRoomPass(room, pass))
                                    {
                                        // Allow join 
                                        AddUserToRoom(room, clientLogin);
                                        stream.WriteByte(0); // Success
                                        Program.LogEvent(
                                            string.Format(
                                                "User '{0}' joined room '{1}' with pass '{2}'", 
                                                clientLogin, 
                                                room, 
                                                pass));
                                    }
                                    else
                                    {
                                        stream.WriteByte(1); // Room Exist, invalid pass
                                        Program.LogEvent(
                                            string.Format(
                                                "User '{0}' failed to join room '{1}' because invalid pass '{2}'", 
                                                clientLogin, 
                                                room, 
                                                pass));
                                    }
                                }
                                else
                                {
                                    // Room doesn't exist
                                    AddRoom(room, pass);
                                    AddUserToRoom(room, clientLogin);
                                    stream.WriteByte(0); // Success
                                    Program.LogEvent(
                                        string.Format(
                                            "User '{0}' joined new room '{1}' with pass '{2}'", 
                                            clientLogin, 
                                            room, 
                                            pass));
                                }

                                break;
                            case 7: // Logout //user - de.Key, room.users - de.Key, if room empty -> delete
                                stream.WriteByte(0); // approve - need?

                                // Free Resources
                                ClientsToFree.Add(clientLogin);
                                FreeTCPClient(client);
                                Program.LogEvent(string.Format("Client '{0}' performed Logout", clientLogin));
                                break;
                            case 8: // Get Rooms                                        
                                data = FormatGetRoomsMsgReply(RoomBase.Keys);
                                stream.WriteByte(8);
                                WriteWrappedMsg(stream, data);
                                Program.LogEvent(string.Format("Client '{0}' requested rooms", clientLogin));
                                break;
                            case 9: // Leave room
                                data = ReadWrappedMsg(stream);
                                string leaveroom = ParseLeaveRoomMsg(data);
                                RemoveClientFromRoom(clientLogin, leaveroom);
                                stream.WriteByte(0); // approve - need?
                                Program.LogEvent(
                                    string.Format("Client '{0}' leaved room '{1}'", clientLogin, leaveroom));
                                break;
                            case 11: // Get Room users
                                string roomname = System.Text.Encoding.UTF8.GetString(ReadWrappedMsg(stream));
                                data = FormatRoomUsers(roomname);
                                stream.WriteByte(11);
                                WriteWrappedMsg(stream, data);
                                Program.LogEvent(string.Format("Client '{0}' requested room users", clientLogin));
                                break;
                            default: // Invalid message from client
                                throw new Exception("Client send unknown token");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Invalid data from current client
                        Program.LogEvent(
                            string.Format(
                                "Received invalid data from client with login '{1}', IP '{2}'-> kick.{0}Reason:{0}{3}", 
                                Environment.NewLine, 
                                clientLogin, 
                                Utils.TCPClient2IPAddress(client), 
                                ex));
                        ClientsToFree.Add(clientLogin);
                        FreeTCPClient(client);
                    }
                }
            }
        }

        #endregion

        #region Rooms&Clients in local datastore

        private static bool IsLogged(string login)
        {
            return ClientBase.ContainsKey(login);
        }

        private static bool AddRoom(string room, string pass)
        {
            if (!RoomBase.Contains(room))
            {
                RoomParams newrp = new RoomParams();
                newrp.Password = pass;
                RoomBase.Add(room, newrp);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void AddUserToRoom(string room, string login)
        {
            ((ChatClient)ClientBase[login]).Rooms.Add(room);
            ((RoomParams)RoomBase[room]).Users.Add(login);
        }

        private static void removeClient(string login)
        {
            // Removes client from cliBase and every room, if room empty -> free it
            ClientBase.Remove(login);
            foreach (DictionaryEntry de in RoomBase)
            {
                RoomParams rp = (RoomParams)de.Value;
                rp.Users.Remove(login);
                if (rp.Users.Count == 0)
                {
                    RoomsToFree.Add((string)de.Key);
                }
            }

            foreach (string froom in RoomsToFree)
            {
                RoomBase.Remove(froom);
            }

            RoomsToFree.Clear();
        }

        private static void RemoveClientFromRoom(string login, string room)
        {
            ((ChatClient)ClientBase[login]).Rooms.Remove(room);
            ((RoomParams)RoomBase[room]).Users.Remove(login);
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
        private static bool RoomExist(string room)
        {
            return RoomBase.ContainsKey(room);
        }

        private static bool confirmRoomPass(string room, string pass)
        {
            return ((RoomParams)RoomBase[room]).Password == pass;
        }

        #endregion

        #region Formatting Messages

        private static byte[] FormatChatMsg(byte msgtype, string source, string dest, string message)
        {
            // dest sometimes is "all"
            // int headerSize = 13;
            byte[] sourceB = System.Text.Encoding.UTF8.GetBytes(source);
            byte[] destB = System.Text.Encoding.UTF8.GetBytes(dest);
            byte[] messageB = System.Text.Encoding.UTF8.GetBytes(message);
            int dataSize = sourceB.Length + destB.Length + messageB.Length;

            // Header - 1+4+4+4         
            byte[] msg = new byte[13 + dataSize];
            msg[0] = msgtype;
            BitConverter.GetBytes(sourceB.Length).CopyTo(msg, 1);
            BitConverter.GetBytes(destB.Length).CopyTo(msg, 5);
            BitConverter.GetBytes(messageB.Length).CopyTo(msg, 9);

            // data
            sourceB.CopyTo(msg, 13);
            destB.CopyTo(msg, 13 + sourceB.Length);
            messageB.CopyTo(msg, 13 + sourceB.Length + destB.Length);
            return msg;
        }

        private static byte[] FormatGetRoomsMsgReply(string[] rooms)
        {
            int i, n = rooms.Length;
            byte[][] roomB = new byte[n][];
            int msgLen = 1 + 4; // type+roomCount
            for (i = 0; i < n; i++)
            {
                roomB[i] = System.Text.Encoding.UTF8.GetBytes(rooms[i]);
                msgLen += 4 + roomB.Length;
            }

            // Formatting Message
            byte[] data = new byte[msgLen];
            data[0] = 0; // type
            byte[] roomCntB = BitConverter.GetBytes(n);
            roomCntB.CopyTo(data, 1);
            int pos = 5;
            byte[] roomBSize;
            for (i = 0; i < n; i++)
            {
                roomBSize = BitConverter.GetBytes(roomB.Length);
                roomBSize.CopyTo(data, pos);
                pos += 4;
                roomB[i].CopyTo(data, pos);
                pos += roomB[i].Length;
            }

            return data;
        }

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

        private static byte[] FormatRoomUsers(string room)
        {
            RoomParams roomParams = (RoomParams)RoomBase[room];

            // foreach (string roomUsr in roomParams.users)
            List<string> users = roomParams.Users;

            int i, n = users.Count;

            byte[][] usrB = new byte[n][];
            int msgLen = 1 + 4; // type+roomCount
            for (i = 0; i < n; i++)
            {
                usrB[i] = System.Text.Encoding.UTF8.GetBytes(users[i]);
                msgLen += 4 + usrB[i].Length;
            }

            // Formatting Message
            byte[] data = new byte[msgLen];
            data[0] = 0; // type
            byte[] usrCntB = BitConverter.GetBytes(n);
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
            byte[] data = new byte[4];
            stream.Read(data, 0, data.Length);
            return BitConverter.ToInt32(data, 0);
        }

        public static byte[] ReadWrappedMsg(NetworkStream stream)
        {
            int streamDataSize = ReadInt32(stream);
            byte[] streamData = new byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return streamData;
        }

        private static int ReadWrappedMsg2(NetworkStream stream, ref byte[] read)
        {
            int streamDataSize = ReadInt32(stream);
            if (streamDataSize >= read.Length)
            {
                int readSZ = stream.Read(read, 0, streamDataSize);
                return readSZ;
            }
            else
            {
                throw new ArgumentException("Too small to read incoming data", "read");
            }
        }

        private static byte[] ReadWrappedEncMsg(NetworkStream stream, AESCSPImpl cryptor)
        {
            int streamDataSize = ReadInt32(stream);
            byte[] streamData = new byte[streamDataSize];
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