using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;
using CustomCrypto;

namespace MyChatServer
{
    public static class ChatServer
    {
        #region Fields

        //public static System.Collections.Hashtable loginBase = new System.Collections.Hashtable(3);//login/pass
        public static System.Collections.Hashtable clientBase = new System.Collections.Hashtable(10);//login/ClientParams
		public static System.Collections.Hashtable roomBase = new System.Collections.Hashtable(3);//room/RoomParams
        const int agrlen = 32;

        private static System.Threading.Thread ListenerThread=null;
        private static MyChatServer.ChatServerDataSetTableAdapters.LoginsTableAdapter loginsTableAdapter;

        private static List<string> clientsToFree = new List<string>(5);
        private static List<string> roomsToFree = new List<string>(5);

        private static bool continueToListen=true;

        public static readonly byte[] staticServerPrivKey = { 71, 177, 16, 173, 145, 214, 65, 103, 205, 32, 107, 19, 241, 223, 113, 87, 172, 178, 195, 75, 171, 208, 130, 47, 94, 231, 207, 220, 175, 147 };
        public static readonly byte[] staticClientPubKey = { 4, 99, 1, 55, 9, 242, 97, 187, 246, 226, 134, 61, 17, 155, 222, 10, 51, 13, 189, 232, 245, 186, 228, 228, 238, 99, 35, 125, 165, 38, 99, 67, 134, 36, 246, 134, 76, 217, 117, 135, 70, 63, 208, 9, 252, 2, 81, 227, 196, 2, 19, 112, 228, 245, 86, 190, 33, 150, 25, 166, 41 };

        static ECDSAWrapper staticDsaClientChecker;//checks with staticClientPubKey
        static ECDSAWrapper staticDsaServerSigner;//signs with staticServerPrivKey

        static byte[] iv1 = { 111, 62, 131, 223, 199, 122, 219, 32, 13, 147, 249, 67, 137, 161, 97, 104 };

        //static ECDSAWrapper seanceDsaClientChecker;//checks client's messages
        //static ECDSAWrapper seanceDsaServerSigner;//signs servers messages

        #endregion

        #region Init&Free

        public static void init(MyChatServer.ChatServerDataSetTableAdapters.LoginsTableAdapter loginsTableAdapter1)
        {
            loginsTableAdapter = loginsTableAdapter1;
            initStaticDSA();
            clientBase.Clear();
            //Listener thread
            if(ListenerThread!=null)            
                ListenerThread.Abort();
            ListenerThread = new System.Threading.Thread(new System.Threading.ThreadStart(ChatServer.Listen));
            ListenerThread.Priority = System.Threading.ThreadPriority.Lowest;
            ListenerThread.Start();
            Program.LogEvent("Listening started");
        }

        public static void finish()
        {
            if (ListenerThread != null)
            {
                continueToListen = false;
                ListenerThread.Join();                
            }
        }

        public static void finish2()
        {
            if (ListenerThread != null)
            {
                continueToListen = false;                
                ListenerThread.Abort();
            }
        }

        public static void freeTCPClient(TcpClient client)
        {
            if (client != null)
            {
                if (client.Connected)
                    client.GetStream().Close();
                client.Close();                
            }
        }

        #endregion

        #region Listen

        public static void Listen()
        {
            TcpListener server = null;
            try
            {                
                Int32 port = MyChatServer.Properties.Settings.Default.Port;
                System.Net.IPAddress localAddr = System.Net.IPAddress.Any;//System.Net.IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);
                server.Start();                
                while (continueToListen)
                {
                    if (server.Pending())
                    {
                        TcpClient client = server.AcceptTcpClient();
                        processPendingConnection(client);
                    }
                    else//Processing current connections
                    {
                        foreach (System.Collections.DictionaryEntry de in clientBase)
                        {                            
                            processCurrentConnection(de);
                        }
                        //free resources from logout of clients
                        foreach (string flogin in clientsToFree)
                            removeClient(flogin);
                        clientsToFree.Clear();
                        //Free unocupied rooms - deprecated because of saving room params (password)
                        //cleanupRooms();
                    }
                }
            }
            catch (Exception e)
            {
                Program.LogException(e);
            }
            finally
            {
                server.Stop();
                Program.LogEvent("Listening finished");
            }            
        }

        static void processPendingConnection(TcpClient client)
        {
            byte[] bytes;
            System.Net.IPAddress ipAddress = TCPClient2IPAddress(client);
            Program.LogEvent(String.Format("Connected from {0}", ipAddress));
            NetworkStream stream = client.GetStream();
            stream.ReadTimeout = 1000;
            try
            {
                int authatt=processAuth(stream);
                if (authatt == 0)
                {
                    AESCSPImpl cryptor;
                    if (processAgreement(stream, out cryptor)==0)
                    {
                        Byte type = (byte)stream.ReadByte();
                        string login, pass;
                        switch (type)
                        {
                            case 0:
                                //Logon attempt
                                bytes = readWrappedEncMsg(stream, cryptor);
                                parseLogonMsg(bytes, out login, out pass);
                                if (inBaseLogPass(login, pass))
                                    if (isLogged(login))
                                    {
                                        ClientParams oldUP = (ClientParams)clientBase[login];
                                        int oldresp = -2;
                                        if (oldUP.client.Connected)
                                        {
                                            NetworkStream oldStream = oldUP.client.GetStream();

                                            try
                                            {
                                                oldStream.WriteByte(10);
                                                oldresp = oldStream.ReadByte();
                                            }
                                            catch (System.IO.IOException)
                                            {
                                                //Timeout - old client probably dead
                                            }
                                        }

                                        if (oldresp == 10)
                                        {
                                            //Client with login <login> still alive -> new login attempt invalid
                                            stream.WriteByte(1);
                                            freeTCPClient(client);
                                            Program.LogEvent(string.Format("Logon from IP '{0}' failed: User '{1}' already logged on", ipAddress, login));
                                        }
                                        else
                                        {
                                            //old client with login <login> dead -> dispose of him and connect new
                                            freeTCPClient(oldUP.client);
                                            removeClient(login);
                                            processAndAcceptNewClient(client, login, cryptor);
                                            Program.LogEvent(string.Format("Logon from IP '{0}' success: User '{1}' from IP  logged on (old client disposed)", ipAddress, login));
                                        }
                                    }
                                    else
                                    {
                                        processAndAcceptNewClient(client, login, cryptor);
                                        Program.LogEvent(string.Format("Logon from IP '{0}' success: User '{1}' from IP  logged on", ipAddress, login));
                                    }
                                else
                                {
                                    stream.WriteByte(2);
                                    freeTCPClient(client);
                                    Program.LogEvent(string.Format("Logon from IP '{0}' failed: Login '{1}'//Password not recognized", ipAddress, login));
                                }
                                break;
                            case 1:
                                //Registration without logon
                                bytes = readWrappedEncMsg(stream, cryptor);
                                parseLogonMsg(bytes, out login, out pass);
                                if (!isLoginInBase(login))
                                {
                                    addLoginPass2Base(login, pass);
                                    stream.WriteByte(0);
                                    Program.LogEvent(string.Format("Registration success: User '{0}' registered", login));
                                }
                                else
                                {
                                    stream.WriteByte(1);
                                    Program.LogEvent(string.Format("Registration failed: User '{0}' already registered", login));
                                }
                                freeTCPClient(client);
                                break;
                            default:
                                //Wrong data received
                                throw new Exception();
                        }
                    }
                }
                else if (authatt == 1)
                {
                    freeTCPClient(client);
                    Program.LogEvent(string.Format("Auth from IP '{0}' fail because client is not legit", ipAddress));
                    //Ban IP...
                }
                else
                {
                    freeTCPClient(client);
                    Program.LogEvent(string.Format("Auth from IP '{0}' fail because error. See previous message for details", ipAddress));                    
                }                    
            }
            catch (Exception ex)
            {
                Program.LogException(new Exception(String.Format("New connetion from IP {0} failed",
                    ipAddress), ex));
                freeTCPClient(client);
                //Ban IP ipAddress...
            }
        }

        

        static void sendData2Stream(NetworkStream stream, byte type, byte[] data)
        {
            stream.WriteByte(type);
            writeWrappedMsg(stream, data);
        }

        #endregion

        #region Processors

        internal static void processAndAcceptNewClient(TcpClient client, string login, AESCSPImpl cryptor1)
        {
            ClientParams newUP = new ClientParams();
            newUP.client = client;
            newUP.cryptor = cryptor1;
            clientBase.Add(login, newUP);
            client.GetStream().WriteByte(0);            
        }

        /// <summary>
        /// Processes authentification attempt from new client
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>0 if ok, 1 if wrong, 2 if exception</returns>
        internal static int processAuth(NetworkStream stream)
        {
            try
            {
                //Check if client is legit
                byte[] send = genSecRandomBytes(100);
                writeWrappedMsg(stream, send);
                byte[] rec = readWrappedMsg(stream);
                //Program.LogEvent(HexRep.ToString(rec));
                bool clientLegit = staticDsaClientChecker.verifyHash(send, rec);
                if (clientLegit)
                {
                    //Clients want to know if server is legit
                    rec = readWrappedMsg(stream);
                    send = staticDsaServerSigner.signHash(rec);
                    writeWrappedMsg(stream, send);
                    return 0;
                }
                else
                    return 1;
            }
            catch (Exception ex)
            {
                Program.LogEvent(String.Format("Error while authentificating: {0}{1}", Environment.NewLine, ex));
                return 2;
            }
        }

        internal static int processAgreement(NetworkStream stream, out AESCSPImpl cryptor)
        {
            try
            {
                ECDHWrapper ecdh1 = new ECDHWrapper(agrlen);
                byte[] recCliPub = readWrappedMsg(stream);
                writeWrappedMsg(stream, ecdh1.PubData);                
                byte[] agr = ecdh1.calcAgreement(recCliPub);

                int aeskeylen = 32;
                byte[] aeskey = new byte[aeskeylen];
                Array.Copy(agr, 0, aeskey, 0, aeskeylen);

                cryptor = new AESCSPImpl(aeskey, iv1);                
                return 0;
            }
            catch (Exception ex)
            {
                Program.LogEvent(String.Format("Error while completing agreement: {0}{1}", Environment.NewLine, ex));
                cryptor = null;
                return 1;
            }
        }

        static void processCurrentConnection(System.Collections.DictionaryEntry de)
        {
            ClientParams clientParams = (ClientParams)de.Value;            
            TcpClient client = clientParams.client;
            if (client != null && client.Connected)
            {
                string clientLogin = (string)de.Key;
                NetworkStream stream = client.GetStream();
                if (stream.DataAvailable)
                {
                    try
                    {
                        //Parsing data from client
                        Byte[] data;
                        int type = stream.ReadByte();
                        string source, dest, messg;
                        switch (type)
                        {
                            case 3://Message to room
                                data = readWrappedEncMsg(stream, clientParams.cryptor);
                                parseChatMsg(data, out source, out dest, out messg);//dest - room
                                Program.LogEvent(string.Format("<Room>[{0}]->[{1}]: \"{2}\"", source, dest, messg));
                                //if user(source) in room(dest)
                                ClientParams senderUP = (ClientParams)clientBase[source];
                                if (senderUP.rooms.Contains(dest))
                                {
                                    RoomParams roomParams = (RoomParams)roomBase[dest];
                                    foreach (string roomUsr in roomParams.users)
                                    {
                                        ClientParams destUP = (ClientParams)clientBase[roomUsr];
                                        if (destUP.client.Connected)
                                        {
                                            try
                                            {
                                                NetworkStream destStream = destUP.client.GetStream();
                                                destStream.WriteByte(3);
                                                writeWrappedEncMsg(destStream, data, destUP.cryptor);
                                            }
                                            catch (System.IO.IOException)
                                            {                                                
                                                clientsToFree.Add(roomUsr);
                                            }
                                        }
                                    }
                                }
                                else//client not in the room he marked as dest
                                { 
                                    stream.WriteByte(1);
                                }
                                break;
                            case 4://Message to user
                                data = readWrappedEncMsg(stream, clientParams.cryptor);
                                parseChatMsg(data, out source, out dest, out messg);//dest - user
                                Program.LogEvent(string.Format("<User>[{0}]->[{1}]: \"{2}\"", source, dest, messg));
                                if (clientBase.Contains(dest))
                                {
                                    ClientParams destUP = (ClientParams)clientBase[dest];//Destination user parameters
                                    if (destUP.client.Connected)
                                    {
                                        try
                                        {
                                            NetworkStream destStream = destUP.client.GetStream();
                                            destStream.WriteByte(4);
                                            writeWrappedEncMsg(destStream, data, destUP.cryptor);
                                        }
                                        catch (System.IO.IOException)
                                        {                                            
                                            clientsToFree.Add(dest);
                                        }  
                                    }
                                }
                                else//no Success - No Such Dest
                                {
                                    stream.WriteByte(1);
                                }
                                break;
                            case 5://Message to All
                                data = readWrappedEncMsg(stream, clientParams.cryptor);
                                //Display to all
                                parseChatMsg(data, out source, out dest, out messg);
                                Program.LogEvent(string.Format("<All>[{0}]->[{1}]: \"{2}\"", source, dest, messg));
                                foreach (System.Collections.DictionaryEntry destDE in clientBase)
                                {
                                    ClientParams destUP = (ClientParams)destDE.Value;
                                    if (destUP.client.Connected)
                                    {
                                        try
                                        {
                                            NetworkStream destStream = destUP.client.GetStream();
                                            destStream.WriteByte(5);
                                            writeWrappedEncMsg(destStream, data, destUP.cryptor);
                                        }
                                        catch (System.IO.IOException)
                                        {                                            
                                            clientsToFree.Add((string)destDE.Key);
                                        }
                                    }                                    
                                }
                                break;
                            case 6://Join Room
                                string room, pass;
                                data = readWrappedEncMsg(stream, clientParams.cryptor);
                                parseJoinRoomMsg(data, out room, out pass);
                                if (roomExist(room))
                                {
                                    if (confirmRoomPass(room, pass))//Allow join 
                                    {
                                        addUserToRoom(room, clientLogin);
                                        stream.WriteByte(0);//Success
                                        Program.LogEvent(string.Format("User '{0}' joined room '{1}' with pass '{2}'", clientLogin, room, pass));
                                    }
                                    else
                                    {
                                        stream.WriteByte(1);//Room Exist, invalid pass
                                        Program.LogEvent(string.Format("User '{0}' failed to join room '{1}' because invalid pass '{2}'", clientLogin, room, pass));
                                    }
                                }
                                else//Room doesn't exist
                                {
                                    addRoom(room, pass);
                                    addUserToRoom(room, clientLogin);
                                    stream.WriteByte(0);//Success
                                    Program.LogEvent(string.Format("User '{0}' joined new room '{1}' with pass '{2}'", clientLogin, room, pass));
                                }
                                break;
                            case 7://Logout //user - de.Key, room.users - de.Key, if room empty -> delete
                                stream.WriteByte(0);//approve - need?
                                //Free Resources
                                clientsToFree.Add(clientLogin);
                                freeTCPClient(client);
                                Program.LogEvent(string.Format("Client '{0}' performed Logout", clientLogin));
                                break;
                            case 8://Get Rooms                                        
                                data = formatGetRoomsMsgReply(roomBase.Keys);
                                stream.WriteByte(8);
                                writeWrappedMsg(stream, data);
                                Program.LogEvent(string.Format("Client '{0}' requested rooms", clientLogin));
                                break;
                            case 9://Leave room
                                data = readWrappedMsg(stream);
                                string leaveroom = parseLeaveRoomMsg(data);
                                removeClientFromRoom(clientLogin, leaveroom);
                                stream.WriteByte(0);//approve - need?
                                Program.LogEvent(string.Format("Client '{0}' leaved room '{1}'", clientLogin, leaveroom));
                                break;
                            case 11://Get Room users
                                string roomname = System.Text.Encoding.UTF8.GetString(readWrappedMsg(stream));
                                data = formatRoomUsers(roomname);
                                stream.WriteByte(11);
                                writeWrappedMsg(stream, data);
                                Program.LogEvent(string.Format("Client '{0}' requested room users", clientLogin));
                                break;
                            default://Invalid message from client
                                throw new Exception("Client send unknown token");
                        }
                    }
                    catch (Exception ex) //Invalid data from current client
                    {
                        Program.LogEvent(String.Format("Received invalid data from client with login '{1}', IP '{2}'-> kick.{0}Reason:{0}{3}",
                            Environment.NewLine, clientLogin, TCPClient2IPAddress(client), ex));
                        clientsToFree.Add(clientLogin);
                        freeTCPClient(client);
                    }
                }
            }
        }

        #endregion

        #region BaseLocal

        static bool inBaseLogPass(string login, string password)
        { return (int)loginsTableAdapter.ValidateLogPass(login, password)>0; }

        static bool isLoginInBase(string login)
        { return (int)loginsTableAdapter.isLoginInBase(login) > 0; }

        static void addLoginPass2Base(string login, string password)
        { loginsTableAdapter.addNewLoginPass(login, password); }        

        static bool isLogged(string login)
        { return clientBase.Contains(login); }

        #endregion
		
		#region RoomsLocal

        static bool addRoom(string room, string pass)
		{
			if(!roomBase.Contains(room)) 
            {
                RoomParams newrp=new RoomParams();
                newrp.password=pass;
                roomBase.Add(room, newrp);
                return true;                
            }
			else return false;
		}
		
		static void addUserToRoom(string room, string login)
        {
            ((ClientParams)clientBase[login]).rooms.Add(room);
            ((RoomParams)roomBase[room]).users.Add(login);
        }

        static void removeClient(string login)//Removes client from cliBase and every room, if room empty -> free it
        {
            clientBase.Remove(login);
            foreach (System.Collections.DictionaryEntry de in roomBase)
            {
                RoomParams rp = ((RoomParams)de.Value);
                rp.users.Remove(login);
                if (rp.users.Count == 0) roomsToFree.Add((string)de.Key);
            }
            foreach (string froom in roomsToFree)
                roomBase.Remove(froom);
            roomsToFree.Clear();
        }

        static void removeClientFromRoom(string login, string room)
        {
            ((ClientParams)clientBase[login]).rooms.Remove(room);
            ((RoomParams)roomBase[room]).users.Remove(login);
        }

        static void cleanupRooms()
        {
            foreach (System.Collections.DictionaryEntry de in roomBase)
            {
                RoomParams rp = ((RoomParams)de.Value);
                if (rp.users.Count == 0) roomsToFree.Add((string)de.Key);
            }
            foreach (string froom in roomsToFree)
                roomBase.Remove(froom);
            roomsToFree.Clear();
        }

        static bool roomExist(string room)
        {
            return roomBase.ContainsKey(room);
        }

        static bool confirmRoomPass(string room, string pass)
        {
            return ((RoomParams)roomBase[room]).password == pass;
        }
		
		#endregion

        #region Formatting Messages

        static Byte[] formatChatMsg(Byte msgtype, string source, string dest, string message)//dest sometimes is "all"
        {
            //int headerSize = 13;
            Byte[] sourceB = System.Text.Encoding.UTF8.GetBytes(source);            
            Byte[] destB = System.Text.Encoding.UTF8.GetBytes(dest);            
            Byte[] messageB = System.Text.Encoding.UTF8.GetBytes(message);
            int dataSize = sourceB.Length + destB.Length + messageB.Length;
            //Header - 1+4+4+4         
            Byte[] msg = new Byte[13+dataSize];
            msg[0] = msgtype;
            BitConverter.GetBytes(sourceB.Length).CopyTo(msg, 1);
            BitConverter.GetBytes(destB.Length).CopyTo(msg, 5);
            BitConverter.GetBytes(messageB.Length).CopyTo(msg, 9);
            //data
            sourceB.CopyTo(msg, 13);
            destB.CopyTo(msg, 13 + sourceB.Length);
            messageB.CopyTo(msg, 13 + sourceB.Length + destB.Length);
            return msg;
        }
		
		static Byte[] formatGetRoomsMsgReply(string[] rooms)
		{
			int i,n=rooms.Length;
			Byte[][] roomB = new Byte[n][];
			int msgLen=1+4;//type+roomCount
			for(i=0;i<n;i++)
			{
				roomB[i]=System.Text.Encoding.UTF8.GetBytes(rooms[i]);
				msgLen+=4+roomB.Length;				
			}
			//Formatting Message
			Byte[] data = new Byte[msgLen];
			data[0]=0;//type
			Byte[] roomCntB = BitConverter.GetBytes(n);
			roomCntB.CopyTo(data, 1);
			int pos=5;
			Byte[] roomBSize;
			for(i=0;i<n;i++)
			{
				roomBSize=BitConverter.GetBytes(roomB.Length);
				roomBSize.CopyTo(data, pos);
				pos+=4;
				roomB[i].CopyTo(data, pos);
				pos+=roomB[i].Length;
			}
			return data;
		}

        static Byte[] formatGetRoomsMsgReply(System.Collections.ICollection rooms)
        {
            int i, n = rooms.Count;
            IEnumerator enmr = rooms.GetEnumerator();
            enmr.Reset();
            Byte[][] roomB = new Byte[n][];
            int msgLen = 1 + 4;//type+roomCount
            for (i = 0; i < n; i++)
            {
                enmr.MoveNext();
                string s = (string)enmr.Current;
                roomB[i] = System.Text.Encoding.UTF8.GetBytes(s);                
                msgLen += 4 + roomB[i].Length;
            }
            //Formatting Message
            Byte[] data = new Byte[msgLen];
            data[0] = 0;//type
            Byte[] roomCntB = BitConverter.GetBytes(n);
            roomCntB.CopyTo(data, 1);
            int pos = 5;
            Byte[] roomBiSize;
            for (i = 0; i < n; i++)
            {
                roomBiSize = BitConverter.GetBytes(roomB[i].Length);//4 bytes
                roomBiSize.CopyTo(data, pos);
                pos += 4;
                roomB[i].CopyTo(data, pos);
                pos += roomB[i].Length;
            }
            return data;
        }

        static Byte[] formatRoomUsers(string room)
        {
            RoomParams roomParams = (RoomParams)roomBase[room];
            //foreach (string roomUsr in roomParams.users)
            List<string> users=roomParams.users;

            int i, n = users.Count;
            
            Byte[][] usrB = new Byte[n][];
            int msgLen = 1 + 4;//type+roomCount
            for (i = 0; i < n; i++)
            {
                usrB[i] = System.Text.Encoding.UTF8.GetBytes(users[i]);
                msgLen += 4 + usrB[i].Length;
            }
            //Formatting Message
            Byte[] data = new Byte[msgLen];
            data[0] = 0;//type
            Byte[] usrCntB = BitConverter.GetBytes(n);
            usrCntB.CopyTo(data, 1);
            int pos = 5;            
            for (i = 0; i < n; i++)
            {
                BitConverter.GetBytes(usrB[i].Length).CopyTo(data, pos);;//4 bytes                
                pos += 4;
                usrB[i].CopyTo(data, pos);
                pos += usrB[i].Length;
            }
            return data;
        }

        #endregion

        #region Parsing

        static void parseLogonMsg(Byte[] bytes, out string login, out string pass)
        {
            //Read header = type+loginSize+passSize (9 bytes)
            int hsz = 9;
            //bytes[0] must be 0
            int loginSize = BitConverter.ToInt32(bytes, 1);
            int passSize = BitConverter.ToInt32(bytes, 5);
            //Read data
            login = System.Text.Encoding.UTF8.GetString(bytes, hsz, loginSize);
            pass = System.Text.Encoding.UTF8.GetString(bytes, hsz + loginSize, passSize);
        }

		static void parseChatMsg(Byte[] bytes, out string source, out string dest, out string message)
        {
            //bytes[0] must be 3 or 4 or 5
            //header Size =13
            //Reading header
            int sourceBSize = BitConverter.ToInt32(bytes, 1);
            int destBSize = BitConverter.ToInt32(bytes, 5);
            int messageBSize = BitConverter.ToInt32(bytes, 9);
            //Reading data                      
            source = System.Text.Encoding.UTF8.GetString(bytes, 13, sourceBSize);
            dest = System.Text.Encoding.UTF8.GetString(bytes, 13 + sourceBSize, destBSize);
            message = System.Text.Encoding.UTF8.GetString(bytes, 13 + sourceBSize + destBSize, messageBSize);
        }

        static void parseJoinRoomMsg(Byte[] bytes, out string room, out string pass)
        {
            //bytes[0] must be 6
            //header Size = 9
            //Reading header
            int roomBlen = BitConverter.ToInt32(bytes, 1);
            int passBlen = BitConverter.ToInt32(bytes, 5);
            //Reading data                      
            room = System.Text.Encoding.UTF8.GetString(bytes, 9, roomBlen);
            pass = System.Text.Encoding.UTF8.GetString(bytes, 9 + roomBlen, passBlen);
        }

        static string parseLeaveRoomMsg(Byte[] bytes)
        {
            int roomBsize = BitConverter.ToInt32(bytes, 0);//reads first 4 bytes - header
            return System.Text.Encoding.UTF8.GetString(bytes, 4, roomBsize);
        }

        #endregion

        #region ReadWrite wrapUnwrap

        static int readInt32(NetworkStream stream)
        {
            Byte[] data = new Byte[4];
            stream.Read(data, 0, data.Length);
            return BitConverter.ToInt32(data, 0);
        }
		
		static Byte[] readWrappedMsg(NetworkStream stream)
        {
            int streamDataSize = readInt32(stream);
            Byte[] streamData = new Byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
			return streamData;
        }

        static int readWrappedMsg2(NetworkStream stream, ref byte[] read)
        {
            int streamDataSize = readInt32(stream);
            if (streamDataSize >= read.Length)
            {
                int readSZ = stream.Read(read, 0, streamDataSize);
                return readSZ;
            }
            else throw new ArgumentException("Too small to read incoming data", "read");
        }

        static Byte[] readWrappedEncMsg(NetworkStream stream, AESCSPImpl cryptor)
        {
            int streamDataSize = readInt32(stream);
            Byte[] streamData = new Byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return cryptor.decrypt(streamData);
        }

        static void writeWrappedMsg(NetworkStream stream, Byte[] bytes)
        {
            Byte[] data = new Byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }

        static void writeWrappedEncMsg(NetworkStream stream, Byte[] plain, AESCSPImpl cryptor)
        {
            byte[] bytes = cryptor.encrypt(plain);
            Byte[] data = new Byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }

        #endregion

        #region Converters

        static System.Net.IPAddress TCPClient2IPAddress(TcpClient client)
        {            
            return (client.Client.RemoteEndPoint as System.Net.IPEndPoint).Address;
        }

        #endregion

        #region OBSOLETE

        /* on init
        loginBase.Clear();
        loginBase.Add("admin", "admpass");
        loginBase.Add("user1", "");
         */

        //static bool inBase(string login, string password)
        //{ return (loginBase.Contains(login) && (loginBase[login] as string) == password); }

        /*
        case 2://Registration with logon - NotApplicable (not needed)
                                bytes = readWrappedMsg(stream);
                                stream.WriteByte(1);
                                if (parseAuthMsg(bytes, out login, out pass))
                                {
                                    if (!loginBase.Contains(login))
                                    {
                                        loginBase.Add(login, pass);
                                        ClientParams cup = new ClientParams();
                                        cup.client = client;
                                        clientBase.Add(login, cup);
                                        stream.WriteByte(0);
                                        Console.WriteLine("Registration+Auth: User '{0}' registered and logged in", login);
                                    }
                                    else
                                    {
                                        stream.WriteByte(1);
                                        Console.WriteLine("Registration+Auth: User '{0}' already registered", login);
                                    }
                                }
                                else
                                    Console.WriteLine("Registration+Auth: Invalid message");
                                break;
        */

        /*
        static bool parseAuthMsg(Byte[] bytes, out string login, out string pass)
        {            
            try
            {
                //Read header = type+loginSize+passSize (9 bytes)
				int hsz=9;
				//bytes[0] must be 0
                int loginSize = BitConverter.ToInt32(bytes, 1);
                int passSize = BitConverter.ToInt32(bytes, 5);
                //Read data
                login = System.Text.Encoding.UTF8.GetString(bytes, hsz, loginSize);
                pass = System.Text.Encoding.UTF8.GetString(bytes, hsz+loginSize, passSize);
            }
            catch (Exception ex)
            {
                login = pass = null;
                Program.LogEvent(String.Format("Invalid Auth Message:/n {0}", ex));                
                return false;
            }
            return true; 
        }
        */

        /*static bool readAuthMsg(NetworkStream stream, out string login, out string pass)
        {            
            try
            {
                //Read header
                Byte[] data = new Byte[8];
                stream.Read(data, 0, 8);
                int loginSize = BitConverter.ToInt32(data, 0);
                int passSize = BitConverter.ToInt32(data, 4);
                //Read data
                data = new Byte[loginSize];
                stream.Read(data, 0, loginSize);
                login = System.Text.Encoding.UTF8.GetString(data);
                data = new Byte[passSize];
                stream.Read(data, 0, passSize);
                pass = System.Text.Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                login = pass = null;
                Console.Error.WriteLine("Invalid Auth Message:/n {0}", ex);
                return false;
            }
            return true; 
        }*/

        /*static void sendGetRoomsMsgReply(NetworkStream stream, string[] rooms)
        {
            stream.WriteByte(0);
            Byte[] data = BitConverter.GetBytes(rooms.Length);
            stream.Write(data, 0, data.Length);
            for(int i=0;i<rooms.Length;i++)
            {
                Byte[] strB = System.Text.Encoding.UTF8.GetBytes(rooms[i]);				
                data = BitConverter.GetBytes(strB.Length);//Length of rooms[i] in bytes
                stream.Write(data, 0, data.Length);
                stream.Write(strB, 0, strB.Length);
            }			
        }*/

        /*static void sendChatMsg(NetworkStream stream, string source, string dest, string message)
        {
            //Header - 1+4+4+4            
            stream.WriteByte(0);
            Byte[] sourceB = System.Text.Encoding.UTF8.GetBytes(source);
            stream.Write(BitConverter.GetBytes(sourceB.Length), 0, 4);
            Byte[] destB = System.Text.Encoding.UTF8.GetBytes(dest);
            stream.Write(BitConverter.GetBytes(destB.Length), 0, 4);
            Byte[] messageB = System.Text.Encoding.UTF8.GetBytes(message);
            stream.Write(BitConverter.GetBytes(messageB.Length), 0, 4);
            //data
            stream.Write(sourceB, 0, sourceB.Length);
            stream.Write(destB, 0, destB.Length);
            stream.Write(messageB, 0, messageB.Length);
        }*/

        #endregion

        #region Helper classes

        public class ClientParams
        {
            //public bool active = true; EQVIVALENT to client==null
            public List<string> rooms = new List<string>(3);//must be list of unique
            public TcpClient client=null;
            //bool freed = false;
            public AESCSPImpl cryptor;
        }

        public class RoomParams
        {
            //public bool isProtected=false; if !protected => pass=""
            public string password = "";//optional, if protected
            public List<string> users = new List<string>(3);//Must be list of unique            
        }

        #endregion

        #region Signing

        private static void initStaticDSA()
        {
            staticDsaServerSigner = new ECDSAWrapper(1, true, staticServerPrivKey);
            staticDsaClientChecker = new ECDSAWrapper(1, false, staticClientPubKey);

            
           
        }

        #endregion

        #region Generators
        /// <summary>
        /// Generates random byte array of set length
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>random byte array of set length</returns>
        static byte[] genRandomBytes(int len)
        {
            Random rand = new Random();
            byte[] bytes = new byte[len];
            rand.NextBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Generates secure random byte array of set length
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>secure random byte array of set length</returns>
        static byte[] genSecRandomBytes(int len)
        {
            Org.BouncyCastle.Security.SecureRandom rand = new Org.BouncyCastle.Security.SecureRandom();
            byte[] bytes = new byte[len];
            rand.NextBytes(bytes);
            return bytes;
        } 
        #endregion
    }
}
