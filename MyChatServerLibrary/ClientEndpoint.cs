namespace Andriy.MyChat.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    using Andriy.MyChat.Server.DAL;
    using Andriy.MyChat.Server.Exceptions;
    using Andriy.Security.Cryptography;

    using global::MyChat.Common.Logging;

    /// <summary>
    /// client == null -> client incative
    /// 
    /// TODO:
    /// 1. implement IDIsposeable to free TcpClient and it's stream
    /// </summary>
    public class ClientEndpoint
    {
        private const int AgreementLength = 32;

        private static readonly ILog Log = LogProvider.GetLogger(typeof(ClientEndpoint));

        private static readonly byte[] CryptoIv1 = { 111, 62, 131, 223, 199, 122, 219, 32, 13, 147, 249, 67, 137, 161, 97, 104 };

        private readonly IServer server;

        private readonly IDataContext dataContext;

        private readonly NetworkStream tcpStream;
        
        /// <summary>
        /// Must be list of unique. TODO: convert type to roomParams
        /// </summary>
        private List<string> rooms = new List<string>(3);

        public ClientEndpoint(IServer server, IDataContext context, TcpClient client)
        {
            this.server = server;
            this.dataContext = context;
            this.Tcp = client;
            this.tcpStream = client.GetStream();
            this.tcpStream.ReadTimeout = 1000; // TODO: remove this from server
            this.CurrentStatus = Status.Uninitialized;
        }

        // TODO: use
        public enum Status
        {
            Uninitialized,
            Verified,
            Encrypted,
            LoggedOn,
            Freed
        }

        public Status CurrentStatus { get; private set; }
        
        public List<string> Rooms
        {
            get
            {
                return this.rooms;
            }
        }

        public TcpClient Tcp { get; set; }
        
        public AESCSPImpl Cryptor { get; set; }

        public string Login { get; set; }

        internal Credentials Credentials { get; private set; }

        // Work in progress
        ////public void ProcessPendingConnection()
        ////{
        ////    IPAddress clientIPAddress;

        ////    try
        ////    {
        ////        clientIPAddress = Utils.TCPClient2IPAddress(this.Tcp);
        ////        Log.DebugFormat("Connected from {0}", clientIPAddress);
        ////        this.ValidateClientApplication();
        ////        this.ProveItself();
        ////        this.InitSecureChannel();
                
        ////        var type = this.ReadByte();
        ////        switch (type)
        ////        {
        ////            case 0:
        ////                // Logon attempt
        ////                this.ReadCredentials();
        ////                if (this.dataContext.ValidateLoginPass(this.Credentials.Login, this.Credentials.Pasword))
        ////                {
        ////                    if (this.IsLogged(chatClient.Credentials.Login))
        ////                    {
        ////                        var existingClient = this.clients[chatClient.Credentials.Login];
        ////                        if (existingClient.PokeForAlive())
        ////                        {
        ////                            // Client with login <login> still alive -> new login attempt invalid
        ////                            clientStream.WriteByte(1);
        ////                            FreeTCPClient(tcp);
        ////                            Log.DebugFormat(
        ////                                    "Logon from IP '{0}' failed: User '{1}' already logged on",
        ////                                    clientIPAddress,
        ////                                    chatClient.Credentials.Login);
        ////                        }
        ////                        else
        ////                        {
        ////                            // old client with login <login> dead -> dispose of him and connect new
        ////                            FreeTCPClient(existingClient.Tcp);
        ////                            this.RemoveClient(chatClient.Credentials.Login);
        ////                            this.ProcessAndAcceptNewClient(tcp, chatClient.Credentials.Login, chatClient.Cryptor);
        ////                            Log.DebugFormat(
        ////                                    "Logon from IP '{0}' success: User '{1}' from IP  logged on (old client disposed)",
        ////                                    clientIPAddress,
        ////                                    chatClient.Credentials.Login);
        ////                        }
        ////                    }
        ////                    else
        ////                    {
        ////                        this.ProcessAndAcceptNewClient(tcp, chatClient.Credentials.Login, chatClient.Cryptor);
        ////                        Log.DebugFormat(
        ////                                "Logon from IP '{0}' success: User '{1}' from IP  logged on",
        ////                                clientIPAddress,
        ////                                chatClient.Credentials.Login);
        ////                    }
        ////                }
        ////                else
        ////                {
        ////                    clientStream.WriteByte(2);
        ////                    FreeTCPClient(tcp);
        ////                    Log.DebugFormat(
        ////                            "Logon from IP '{0}' failed: Login '{1}'//Password not recognized",
        ////                            clientIPAddress,
        ////                            chatClient.Credentials.Login);
        ////                }

        ////                break;
        ////            case 1:

        ////                // Registration without logon
        ////                chatClient.ReadCredentials();
        ////                if (!this.dataContext.LoginExists(chatClient.Credentials.Login))
        ////                {
        ////                    this.dataContext.AddUser(chatClient.Credentials.Login, chatClient.Credentials.Pasword);
        ////                    clientStream.WriteByte(0);
        ////                    Log.DebugFormat("Registration success: User '{0}' registered", chatClient.Credentials.Login);
        ////                }
        ////                else
        ////                {
        ////                    clientStream.WriteByte(1);
        ////                    Log.DebugFormat("Registration failed: User '{0}' already registered", chatClient.Credentials.Login);
        ////                }

        ////                FreeTCPClient(tcp);
        ////                break;
        ////            default:

        ////                // Wrong data received
        ////                throw new Exception();
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        Log.Error(new Exception(string.Format("Client application connected from {0}, but was invalid", clientIPAddress), ex));
        ////        this.FreeTCPClient();

        ////        // Ban IP ipAddress...
        ////    }
        ////}

        public void ProcessCurrentConnection()
        {
            if (this.Tcp == null || !this.Tcp.Connected)
            {
                // TODO: dispose myself, remove from Server
                return;
            }

            string clientLogin = this.Login;
            if (!this.DataAvailable())
            {
                // Just skip
                return;
            }

            try
            {
                // Parsing data from client
                byte[] data;
                int type = this.ReadByte(); // TODO: resolve blocking
                string source, dest, messg;
                switch (type)
                {
                    case 3: // Message to room
                        data = this.ReadWrappedEncMsg();
                        ParseChatMsg(data, out source, out dest, out messg); // dest - room
                        Log.DebugFormat("<Room>[{0}]->[{1}]: \"{2}\"", source, dest, messg);

                        // if user(source) in room(dest)
                        var senderClient = this;
                        if (senderClient.Rooms.Contains(dest))
                        {
                            var roomParams = this.server.GetRoom(dest);
                            foreach (string roomUsr in roomParams.Users)
                            {
                                var destinationClient1 = this.server.GetChatClient(roomUsr);
                                try
                                {
                                    destinationClient1.SendByte(3);
                                    destinationClient1.WriteWrappedEncMsg(data);
                                }
                                catch (IOException)
                                {
                                    this.server.StageClientForRemoval(destinationClient1);
                                }
                            }
                        }
                        else
                        {
                            // client not in the room he marked as dest
                            this.SendByte(1);
                        }

                        break;
                    case 4: // Message to user
                        data = this.ReadWrappedEncMsg();
                        ParseChatMsg(data, out source, out dest, out messg); // dest - user
                        Log.DebugFormat("<User>[{0}]->[{1}]: \"{2}\"", source, dest, messg);
                        var destinationClient = this.server.GetChatClient(dest);
                        if (destinationClient != null)
                        {
                            try
                            {
                                destinationClient.SendByte(4);
                                destinationClient.WriteWrappedEncMsg(data);
                            }
                            catch (IOException)
                            {
                                this.server.StageClientForRemoval(destinationClient);
                            }
                        }
                        else
                        {
                            // no Success - No Such Dest
                            this.SendByte(1);
                        }

                        break;
                    case 5: // Message to All
                        data = this.ReadWrappedEncMsg();

                        // Display to all
                        ParseChatMsg(data, out source, out dest, out messg);
                        Log.DebugFormat("<All>[{0}]->[{1}]: \"{2}\"", source, dest, messg);
                        foreach (var destinationClient1 in this.server.GetChatClients())
                        {
                            try
                            {
                                destinationClient1.SendByte(5);
                                destinationClient1.WriteWrappedEncMsg(data);
                            }
                            catch (IOException)
                            {
                                this.server.StageClientForRemoval(destinationClient1);
                            }
                        }

                        break;
                    case 6: // Join Room
                        string room, pass;
                        data = this.ReadWrappedEncMsg();
                        ParseJoinRoomMsg(data, out room, out pass);
                        if (this.server.RoomExist(room))
                        {
                            if (this.server.ConfirmRoomPass(room, pass))
                            {
                                // Allow join 
                                this.server.AddUserToRoom(room, clientLogin);
                                this.SendByte(0); // Success
                                Log.DebugFormat("User '{0}' joined room '{1}' with pass '{2}'", clientLogin, room, pass);
                            }
                            else
                            {
                                this.SendByte(1); // Room Exist, invalid pass
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
                            this.server.TryCreateRoom(room, pass);
                            this.server.AddUserToRoom(room, clientLogin);
                            this.SendByte(0); // Success
                            Log.DebugFormat("User '{0}' joined new room '{1}' with pass '{2}'", clientLogin, room, pass);
                        }

                        break;
                    case 7: // Logout //user - de.Key, room.users - de.Key, if room empty -> delete
                        this.SendByte(0); // Approve
                        this.server.StageClientForRemoval(this); // Free Resources
                        Log.DebugFormat("Client '{0}' performed Logout", clientLogin);
                        break;
                    case 8: // Get Rooms                                        
                        data = FormatGetRoomsMsgReply(this.server.GetRoomsNames().ToArray());
                        this.SendByte(8);
                        this.WriteWrappedMsg(data);
                        Log.DebugFormat("Client '{0}' requested rooms", clientLogin);
                        break;
                    case 9: // Leave room
                        data = this.ReadWrappedMsg();
                        string leaveroom = ParseLeaveRoomMsg(data);
                        this.server.RemoveClientFromRoom(clientLogin, leaveroom);
                        this.SendByte(0); // Approve
                        Log.DebugFormat("Client '{0}' leaved room '{1}'", clientLogin, leaveroom);
                        break;
                    case 11: // Get Room users
                        string roomname = System.Text.Encoding.UTF8.GetString(this.ReadWrappedMsg());
                        data = this.server.FormatRoomUsers(roomname);
                        this.SendByte(11);
                        this.WriteWrappedMsg(data);
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
                    Utils.TCPClient2IPAddress(this.Tcp),
                    ex);
                this.server.StageClientForRemoval(this); // CLient send invalid data, so we'll "drop" him
            }
        }

        /// <summary>
        /// Sets up secure channel (this.Cryptor)
        /// </summary>
        /// <returns></returns>
        public int SetUpSecureChannel()
        {
            try
            {
                var ecdh1 = new ECDHWrapper(AgreementLength);
                var recCliPub = this.ReadWrappedMsg();
                this.WriteWrappedMsg(ecdh1.PubData);
                var agr = ecdh1.calcAgreement(recCliPub);

                const int AESKeyLength = 32;
                var aeskey = new byte[AESKeyLength];
                Array.Copy(agr, 0, aeskey, 0, AESKeyLength);

                this.Cryptor = new AESCSPImpl(aeskey, CryptoIv1);
                return 0;
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error while completing agreement: {0}{1}", Environment.NewLine, ex);
                this.Cryptor = null;
                return 1;
            }
        }

        /// <summary>
        /// Init secure channel (this.Cryptor)
        /// </summary>
        /// <returns></returns>
        private void InitSecureChannel()
        {
            try
            {
                var ecdh1 = new ECDHWrapper(AgreementLength);
                var recCliPub = this.ReadWrappedMsg();
                this.WriteWrappedMsg(ecdh1.PubData);
                var agr = ecdh1.calcAgreement(recCliPub);

                const int AESKeyLength = 32;
                var aeskey = new byte[AESKeyLength];
                Array.Copy(agr, 0, aeskey, 0, AESKeyLength);

                this.Cryptor = new AESCSPImpl(aeskey, CryptoIv1);
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error while completing agreement: {0}{1}", Environment.NewLine, ex);
                this.Cryptor = null;
                throw new SecureChannelInitFailedException(string.Empty, ex);
            }
        }

        private void HandleLoginProcedure()
        {
            
        }

        private static int ReadInt32(NetworkStream stream)
        {
            var data = new byte[4];
            stream.Read(data, 0, data.Length);
            return BitConverter.ToInt32(data, 0);
        }

        private byte[] ReadWrappedMsg()
        {
            var stream = this.Tcp.GetStream();
            int streamDataSize = ReadInt32(stream);
            var streamData = new byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return streamData;
        }

        private byte[] ReadWrappedEncMsg()
        {
            var stream = this.tcpStream;
            int streamDataSize = ReadInt32(stream);
            var streamData = new byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return this.Cryptor.Decrypt(streamData);
        }

        private void WriteWrappedMsg(byte[] bytes)
        {
            var data = new byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            this.tcpStream.Write(data, 0, data.Length);
        }

        private void WriteWrappedEncMsg(byte[] plain)
        {
            var bytes = this.Cryptor.Encrypt(plain);
            var data = new byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            this.tcpStream.Write(data, 0, data.Length);
        }

        public byte ReadByte()
        {
            var res = this.Tcp.GetStream().ReadByte();
            if (res >= 0)
            {
                return (byte)res;
            }
            else
            {
                // res == -1 --> end of stream
                throw new EndOfStreamException();
            }
        }

        private void SendByte(byte value)
        {
            this.Tcp.GetStream().WriteByte(value);
        }

        public void ReadCredentials()
        {
            var bytes = this.ReadWrappedEncMsg();
            var creds = Credentials.Parse(bytes, 1);
            this.Credentials = creds;
        }

        public void FreeTCPClient()
        {
            if (this.Tcp != null)
            {
                if (this.Tcp.Connected)
                {
                    this.Tcp.GetStream().Close();
                    ////this.tcpStream. = null; // TODO
                }

                this.Tcp.Close();
            }

            this.CurrentStatus = Status.Freed;
        }

        public bool PokeForAlive()
        {
            if (!this.Tcp.Connected)
            {
                return false;
            }

            try
            {
                this.tcpStream.WriteByte(10);
                if (this.ReadByte() == 10)
                {
                    return true;
                }
                else
                {
                    // Received unexpected data
                    return false;
                }
            }
            catch (IOException)
            {
                return false;
            }
            
        }

        /// <summary>
        /// Check if client [application] is original, 
        /// i.e. if it has valid private key
        /// </summary> 
        /// <returns>0 if ok, 1 if wrong</returns>
        [Obsolete("THis method is obsolete. Use ValidateClientApplication() instead.")]
        internal int Verify()
        {
            try
            {
                var stream = this.tcpStream;

                // Check if client is legit
                var send = MyRandoms.GenerateSecureRandomBytes(100);
                ChatServer.WriteWrappedMsg(stream, send);
                var rec = ChatServer.ReadWrappedMsg(stream);

                // Program.LogEvent(HexRep.ToString(rec));
                bool clientLegit = Crypto.Utils.ClientVerifier.verifyHash(send, rec);
                if (clientLegit)
                {
                    // Clients want to know if server is legit
                    rec = ChatServer.ReadWrappedMsg(stream);
                    send = Crypto.Utils.ServerSigner.signHash(rec);
                    ChatServer.WriteWrappedMsg(stream, send);
                    this.CurrentStatus = Status.Verified;
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error while authentificating: {0}{1}", Environment.NewLine, ex);
                return 1;
            }
        }

        /// <summary>
        /// Check if client [application] is original, 
        /// i.e. if it has valid private key
        /// </summary> 
        /// <returns>0 if ok, 1 if wrong</returns>
        private void ValidateClientApplication()
        {
            try
            {
                var stream = this.tcpStream;

                // Check if client is legit
                var send = MyRandoms.GenerateSecureRandomBytes(100);
                ChatServer.WriteWrappedMsg(stream, send);
                var rec = ChatServer.ReadWrappedMsg(stream);

                // Program.LogEvent(HexRep.ToString(rec));
                bool clientLegit = Crypto.Utils.ClientVerifier.verifyHash(send, rec);
                if (!clientLegit)
                {
                    throw new ClientProgramInvalidException("Chat client program was not authenticated");
                }
            }
            catch (ClientProgramInvalidException ex)
            {
                Log.DebugFormat("Error while authentificating: {0}",  ex);
                throw;
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error while authentificating: {0}", ex);
                throw new ClientProgramInvalidException(string.Empty, ex);
            }
        }

        private void ProveItself()
        {
            // Clients want to know if server is legit
            var rec = ChatServer.ReadWrappedMsg(this.tcpStream);
            var send = Crypto.Utils.ServerSigner.signHash(rec);
            ChatServer.WriteWrappedMsg(this.tcpStream, send);
            this.CurrentStatus = Status.Verified;
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

        private static byte[] FormatGetRoomsMsgReply(string[] rooms)
        {
            int i, n = rooms.Length;
            var roomB = new byte[n][];
            int msgLen = 1 + 4; // type+roomCount
            for (i = 0; i < n; i++)
            {
                string s = rooms[i];
                roomB[i] = System.Text.Encoding.UTF8.GetBytes(s);
                msgLen += 4 + roomB[i].Length;
            }

            // Formatting Message
            var data = new byte[msgLen];
            data[0] = 0; // type
            byte[] roomCntB = BitConverter.GetBytes(n);
            roomCntB.CopyTo(data, 1);
            int pos = 5;
            for (i = 0; i < n; i++)
            {
                var roomBiSize = BitConverter.GetBytes(roomB[i].Length); // 4 bytes
                roomBiSize.CopyTo(data, pos);
                pos += 4;
                roomB[i].CopyTo(data, pos);
                pos += roomB[i].Length;
            }

            return data;
        }

        private bool DataAvailable()
        {
            if (this.Tcp == null || !this.Tcp.Connected)
            {
                return false;
            }

            var stream = this.Tcp.GetStream();
            if (stream == null)
            {
                return false;
            }

            return stream.DataAvailable;
        }
    }
}