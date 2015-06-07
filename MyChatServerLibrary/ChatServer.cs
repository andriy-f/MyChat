namespace Andriy.MyChat.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using Andriy.MyChat.Server.DAL;

    using global::MyChat.Common.Logging;

    public class ChatServer : IServer
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ChatServer));
        
        // TODO: make concurrent
        private readonly Dictionary<string, ClientEndpoint> clients = new Dictionary<string, ClientEndpoint>(10); // login/ChatClient

        // TODO: make concurrent
        private readonly Dictionary<string, RoomParams> roomBase = new Dictionary<string, RoomParams>(); // room/RoomParams
        
        private readonly List<string> unusedClients = new List<string>(5);

        private readonly List<string> unusedRooms = new List<string>(5);

        private readonly Thread listenerThread;

        private readonly IDataContext dataContext;

        private volatile bool continueToListen = true;

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

        public void Stop()
        {
            this.continueToListen = false;
            
            if (this.listenerThread != null)
            {
                this.listenerThread.Join();
            }
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

        public void AddLoggedInUser(string login, ClientEndpoint clientEndpoint)
        {
            this.clients.Add(login, clientEndpoint);
        }

        public bool IsLoggedIn(string login)
        {
            return this.clients.ContainsKey(login);
        }

        public void AddUserToRoom(string room, string login)
        {
            this.clients[login].Rooms.Add(room);
            this.roomBase[room].Users.Add(login);
        }

        public ClientEndpoint GetChatClient(string login)
        {
            return this.clients[login];
        }

        public IEnumerable<ClientEndpoint> GetChatClients()
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

        public void QueueClientForRemoval(ClientEndpoint clientEndpoint)
        {
            this.unusedClients.Add(clientEndpoint.Login);
        }

        public void FreeClientsQueuedForRemoval()
        {
            foreach (var flogin in this.unusedClients)
            {
                this.RemoveClient(flogin);
            }

            this.unusedClients.Clear();
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
        
        public void RemoveClient(string login)
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
                        var tcpClient = tcpListener.AcceptTcpClient();
                        var clientEndpoint = new ClientEndpoint(this, this.dataContext, tcpClient);
                        clientEndpoint.ProcessPendingConnection();
                    }
                    else
                    {
                        // Processing current connections
                        foreach (var de in this.clients)
                        {
                            de.Value.ProcessCurrentConnection();
                        }

                        this.FreeClientsQueuedForRemoval();

                        // cleanupRooms(); // Free unocupied rooms - deprecated because of saving room params (password)
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString);
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
    }
}