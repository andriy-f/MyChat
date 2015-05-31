namespace Andriy.MyChat.Server
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using Andriy.MyChat.Server.DAL;
    using Andriy.Security.Cryptography;

    using log4net;

    public class ChatServer : IServer
    {
        #region Fields

        private static readonly ILog Log = LogManager.GetLogger(typeof(ChatServer));

        internal static readonly byte[] CryptoIv1 = { 111, 62, 131, 223, 199, 122, 219, 32, 13, 147, 249, 67, 137, 161, 97, 104 };

        // TODO: mace concurrent
        private readonly Dictionary<string, ChatClient> clients = new Dictionary<string, ChatClient>(10); // login/ChatClient

        // TODO: mace concurrent
        private readonly Dictionary<string, RoomParams> roomBase = new Dictionary<string, RoomParams>(); // room/RoomParams
        
        private readonly List<string> unusedClients = new List<string>(5);

        private readonly List<string> unusedRooms = new List<string>(5);

        private Thread listenerThread;

        private readonly IDataContext dataContext;

        private bool continueToListen = true;

        public ChatServer(IDataContext newDataContext, int port)
        {
            this.dataContext = newDataContext;

            this.Port = port;

            // Listener thread
            if (this.listenerThread != null)
            {
                this.listenerThread.Abort();
            }

            this.listenerThread = new Thread(this.Listen)
            {
                Priority = ThreadPriority.Lowest,
                IsBackground = true
            };

            this.listenerThread.Start();
            Log.Info("Listening started");
        }
        
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

        private void Listen()
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
                        this.ProcessPendingConnection(tcpListener.AcceptTcpClient());
                    }
                    else
                    {
                        // Processing current connections
                        foreach (var de in this.clients)
                        {
                            de.Value.ProcessCurrentConnection();
                        }

                        this.FreeClientsStagedForRemoval();

                        // cleanupRooms(); // Free unocupied rooms - deprecated because of saving room params (password)
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

        private void ProcessPendingConnection(TcpClient tcp)
        {
            var clientIPAddress = Utils.TCPClient2IPAddress(tcp);
            Log.DebugFormat("Connected from {0}", clientIPAddress);
            var clientStream = tcp.GetStream();
            clientStream.ReadTimeout = 1000;

            try
            {
                var chatClient = new ChatClient(this, tcp);
                int authatt = chatClient.Verify();
                switch (authatt)
                {
                    case 0:
                        if (chatClient.SetUpSecureChannel() == 0)
                        {
                            var type = chatClient.ReadByte();
                            switch (type)
                            {
                                case 0:
                                    // Logon attempt
                                    chatClient.ReadCredentials();
                                    if (this.dataContext.ValidateLoginPass(chatClient.Credentials.Login, chatClient.Credentials.Pasword))
                                    {
                                        if (this.IsLogged(chatClient.Credentials.Login))
                                        {
                                            var existingClient = this.clients[chatClient.Credentials.Login];
                                            if (existingClient.PokeForAlive())
                                            {
                                                // Client with login <login> still alive -> new login attempt invalid
                                                clientStream.WriteByte(1);
                                                FreeTCPClient(tcp);
                                                Log.DebugFormat(
                                                        "Logon from IP '{0}' failed: User '{1}' already logged on", 
                                                        clientIPAddress,
                                                        chatClient.Credentials.Login);
                                            }
                                            else
                                            {
                                                // old client with login <login> dead -> dispose of him and connect new
                                                FreeTCPClient(existingClient.Tcp);
                                                this.RemoveClient(chatClient.Credentials.Login);
                                                this.ProcessAndAcceptNewClient(tcp, chatClient.Credentials.Login, chatClient.Cryptor);
                                                Log.DebugFormat(
                                                        "Logon from IP '{0}' success: User '{1}' from IP  logged on (old client disposed)", 
                                                        clientIPAddress,
                                                        chatClient.Credentials.Login);
                                            }
                                        }
                                        else
                                        {
                                            this.ProcessAndAcceptNewClient(tcp, chatClient.Credentials.Login, chatClient.Cryptor);
                                            Log.DebugFormat(
                                                    "Logon from IP '{0}' success: User '{1}' from IP  logged on", 
                                                    clientIPAddress,
                                                    chatClient.Credentials.Login);
                                        }
                                    }
                                    else
                                    {
                                        clientStream.WriteByte(2);
                                        FreeTCPClient(tcp);
                                        Log.DebugFormat(
                                                "Logon from IP '{0}' failed: Login '{1}'//Password not recognized", 
                                                clientIPAddress,
                                                chatClient.Credentials.Login);
                                    }

                                    break;
                                case 1:

                                    // Registration without logon
                                    chatClient.ReadCredentials();
                                    if (!this.dataContext.LoginExists(chatClient.Credentials.Login))
                                    {
                                        this.dataContext.AddUser(chatClient.Credentials.Login, chatClient.Credentials.Pasword);
                                        clientStream.WriteByte(0);
                                        Log.DebugFormat("Registration success: User '{0}' registered", chatClient.Credentials.Login);
                                    }
                                    else
                                    {
                                        clientStream.WriteByte(1);
                                        Log.DebugFormat("Registration failed: User '{0}' already registered", chatClient.Credentials.Login);
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
            var chatClient = new ChatClient(this, client);
            chatClient.Login = login;
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
        
        #endregion

        #region Rooms&Clients in local datastore

        private bool IsLogged(string login)
        {
            return this.clients.ContainsKey(login);
        }

        public bool TryCreateRoom(string name, string password)
        {
            if (this.roomBase.ContainsKey(name))
            {
                return false;
            }

            var newrp = new RoomParams { Password = password };
            this.roomBase.Add(name, newrp);
            return true;
        }

        public void AddUserToRoom(string room, string login)
        {
            this.clients[login].Rooms.Add(room);
            this.roomBase[room].Users.Add(login);
        }

        private void RemoveClient(string login)
        {
            this.clients[login].FreeTCPClient();

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

        public void RemoveClientFromRoom(string login, string room)
        {
            this.clients[login].Rooms.Remove(room);
            this.roomBase[room].Users.Remove(login);
        }

        public bool RoomExist(string room)
        {
            return this.roomBase.ContainsKey(room);
        }

        public bool ConfirmRoomPass(string roomName, string password)
        {
            return this.roomBase[roomName].Password == password;
        }

        #endregion

        #region Formatting Messages
        
        public byte[] FormatRoomUsers(string room)
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
        
        public static void WriteWrappedMsg(Stream stream, byte[] bytes)
        {
            var data = new byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }

        #endregion

        public ChatClient GetChatClient(string login)
        {
            return this.clients[login];
        }

        public IEnumerable<ChatClient> GetChatClients()
        {
            return this.clients.Values;
        }

        public RoomParams GetRoom(string name)
        {
            return this.roomBase[name];
        }

        public IEnumerable<string> GetRoomsNames()
        {
            return this.roomBase.Keys;
        }

        public void StageClientForRemoval(ChatClient client)
        {
            this.unusedClients.Add(client.Login);
        }

        public void FreeClientsStagedForRemoval()
        {
            foreach (var flogin in this.unusedClients)
            {
                this.RemoveClient(flogin);
            }

            this.unusedClients.Clear();
        }
    }
}