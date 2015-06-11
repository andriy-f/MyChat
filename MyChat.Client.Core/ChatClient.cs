namespace MyChat.Client.Core
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;

    using Andriy.MyChat.Client;
    using Andriy.Security.Cryptography;

    using global::MyChat.Client.Core.Logging;

    using MyChat.Client.Core.Exceptions;
    using MyChat.Common.Network;

    /// <summary>
    /// -- Queries to server: --
    /// type - in 1st byte
    /// type 0 - authorization(login) answers: 0-success, 1-already logged on, 2-invalid login/pass
    /// type 1 - registration ans: 0-success, 1-already exist, 2 - registration unaviable
    /// type 2 - type 1 + autologin

    /// type 3,4,5 - chat message 
    /// type 6 - join room - if room !exist => create with pass, else check pass
    /// type 7 - logout
    /// type 8 - get rooms
    /// type 9 - leave room  
    /// type 10 - check if alive
    /// typr 11 - get room users
    /// ----------------------------------Messages From Server------------------------------------
    /// type 3 4 5 - incoming message
    /// message header = &lt;type&gt; + sourceBSize+destBSize+messageBSize+(styleSize)
    /// message data   = source, dest, message, (style)
    /// -------------------------------------------------------------------------
    /// Needed improvements:
    /// 1. stream must be accessed only from listenToServer or lock stream while working with it
    /// </summary>
    public class ChatClient
    {
        private const int AgreementLength = 32;

        private static readonly ILog Logger = LogProvider.GetLogger(typeof(ChatClient));

        private static readonly byte[] StaticServerPublicKey = { 4, 81, 97, 253, 33, 148, 211, 27, 164, 103, 98, 244, 190, 246, 165, 216, 112, 148, 56, 28, 38, 55, 92, 241, 130, 210, 62, 81, 127, 210, 78, 3, 95, 35, 72, 221, 34, 5, 200, 194, 215, 102, 191, 60, 52, 30, 164, 242, 52, 255, 64, 199, 132, 23, 249, 234, 50, 171, 242, 160, 223 };

        private static readonly byte[] StaticClientPrivateKey = { 57, 16, 61, 194, 88, 158, 65, 114, 36, 14, 242, 62, 215, 205, 157, 122, 229, 105, 130, 118, 235, 214, 214, 25, 171, 106, 38, 200, 35, 185 };

        private static readonly byte[] Iv1 = { 111, 62, 131, 223, 199, 122, 219, 32, 13, 147, 249, 67, 137, 161, 97, 104 };

        private readonly MsgProcessor messageProcessor = new MsgProcessor();

        private readonly Queue<ListenProcessor> listenQueue = new Queue<ListenProcessor>();

        private readonly Queue<Action> sendQueue = new Queue<Action>();

        private readonly System.Threading.Mutex mut = new System.Threading.Mutex();
        
        private int portNum;

        private string password;
        
        private TcpClient client;

        private NetworkStream stream;

        private System.Threading.Thread listenerThread;
        
        private ECDSAWrapper staticDsaClientSigner;//signs with staticClientPrivKey

        private ECDSAWrapper staticDsaServerChecker;//check with staticServerPubKey 

        private AESCSPImpl cryptor;

        private IStreamWrapper messageFramer;

        ////static ECDSAWrapper seanceDsaClientSigner;//signs client's messages
        ////static ECDSAWrapper seanceDsaServerChecker;//checks servers messages

        public MsgProcessor MessageProcessor
        {
            get
            {
                return this.messageProcessor;
            }
        }

        public string Server { get; private set; }

        public string Login { get; private set; }

        public void Init(string _serv, int _prt, string _login, string _password)
        {
            this.Server = _serv; 
            this.portNum = _prt; 
            this.Login = _login; 
            this.password = _password;
            this.InitStaticDSA();
            this.FreeClient();
            this.client = new TcpClient(this.Server, this.portNum);
            this.stream = this.client.GetStream();
            this.stream.ReadTimeout = 7000;
            this.messageFramer = new FramedProtocol(this.stream);
        }        

        public void StartListener()
        {
            if (this.listenerThread != null)
            {
                this.StopListener();
            }

            this.listenerThread = new System.Threading.Thread(this.ListenToServer)
                                      {
                                          Priority = System.Threading.ThreadPriority.Lowest
                                      };
            this.listenerThread.Start();
            Logger.Info("Listening started");
        }

        public void StopListener()
        {
            this.listenerThread.Abort();
        }

        public void ListenToServer()
        {
            try
            {
                ////throw new IndexOutOfRangeException(); TODO: find way to report to GUI
                while (true)
                {
                    if (this.sendQueue.Count > 0)
                    {
                        Action toSend = this.sendQueue.Dequeue();
                        toSend();
                    }
                    if (this.stream.DataAvailable)
                    {
                        this.mut.WaitOne();
                        Byte[] streamData;// = readWrappedMsg(stream);
                        int type = this.stream.ReadByte();
                        string source, dest, message;
                        switch (type)
                        {
                            case 1:                                
                                this.MessageProcessor.process("Server", "<unknown>", "Previous message was not deivered");
                                break;
                            case 3://Incoming Message for room                                    
                                streamData = this.ReadWrappedEncMsg(this.stream);//streamData[0] must == 3
                                parseChatMsg(streamData, out source, out dest, out message);
                                //displaying Message
                                Logger.Info(string.Format("[{0}] -> [{1}]: \"{2}\"", source, dest, message));
                                this.MessageProcessor.processForRoom(source, dest, message);
                                break;
                            case 4://Incoming Message for user
                                streamData = this.ReadWrappedEncMsg(this.stream);//streamData[0] must == 4
                                parseChatMsg(streamData, out source, out dest, out message);
                                //displaying Message
                                Logger.Info(String.Format("[{0}] -> [{1}]: \"{2}\"", source, dest, message));
                                this.MessageProcessor.process(source, dest, message);
                                break;
                            case 5://Incoming Message for All
                                streamData = this.ReadWrappedEncMsg(this.stream);//streamData[0] must == 5
                                parseChatMsg(streamData, out source, out dest, out message);
                                //displaying Message
                                Logger.Info(String.Format("[{0}] -> [{1}]: \"{2}\"", source, dest, message));
                                this.MessageProcessor.process(source, dest, message);
                                break;
                            case 10:
                                this.stream.WriteByte(10);//I'm alive!
                                break;
                            default:
                                if (this.listenQueue.Count > 0 && type == this.listenQueue.Peek().code)
                                {
                                    this.listenQueue.Dequeue().toDo();
                                    break;
                                }
                                
                                throw new Exception("Server sent unknown token");
                        }

                        this.mut.ReleaseMutex();
                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                Logger.Info("Listener stopped by aborting (normal)");
            }
            catch (Exception ex)
            {
                Logger.Error(new Exception("Error while listening to server", ex).ToString());                
            }
        }

        /// <summary>
        /// Authentification  TODO: use
        /// </summary>
        public void ValidateItselfAndServer()
        {
            try
            {
                // Validate itself
                // Server sends random data which client must sign
                var serverChallenge = this.messageFramer.Receive();
                var challengeAnswer = this.staticDsaClientSigner.signHash(serverChallenge);
                this.messageFramer.Send(challengeAnswer);

                // Now client checks that server is valid
                // Client gen random data which server must sign
                var clientChallenge = GenSecRandomBytes(100);
                this.messageFramer.Send(clientChallenge);
                var serverChallengeAnswer = this.messageFramer.Receive();
                if (!this.staticDsaServerChecker.verifyHash(clientChallenge, serverChallengeAnswer))
                {
                    throw new ServerUntrustedException("Invalid server");
                }
            }
            catch (ServerUntrustedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ServerUntrustedException("Invalid server", ex);
            }
        }

        public bool performAgreement()
        {
            try
            {
                ECDHWrapper ecdh1 = new ECDHWrapper(AgreementLength);
                WriteWrappedMsg(this.stream, ecdh1.PubData);                
                byte[] recSerPub = ReadWrappedMsg(this.stream);
                byte[] agr = ecdh1.calcAgreement(recSerPub);

                int aeskeylen=32;
                byte[] aeskey=new byte[aeskeylen];
                Array.Copy(agr, 0, aeskey, 0, aeskeylen);

                this.cryptor = new AESCSPImpl(aeskey, Iv1);                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Debug(String.Format("Error while completing agreement: {0}{1}", Environment.NewLine, ex));
                return false;
            }
        }

        public int performLogonDef()
        {
            try
            {                
                Byte[] data = ChatClient.formatLogonMsg(this.Login, this.password);
                this.stream.WriteByte(0);//Identifies logon attempt
                this.WriteWrappedEncMsg(this.stream, data);
                int resp = this.stream.ReadByte();//Ans
                switch (resp)
                {
                    case 0://Success
                        Logger.Debug(String.Format("Logon success with login '{0}'", this.Login));                        
                        break;
                    case 1://Already logged on
                        Logger.Debug(String.Format("Logon fail: User '{0}' already logged on", this.Login));
                        this.FreeClient();
                        break;
                    case 2://Invalid login/pass
                        Logger.Debug(String.Format("Logon fail: Invalid login//pass"));
                        this.FreeClient();
                        break;
                }
                return resp;
            }
            catch (ArgumentNullException ex)
            {
                Logger.Error(ex.ToString);                
                return 3;
            }
            catch (SocketException ex)
            {
                Logger.Error(ex.ToString);
                return 4;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString);
                return 5;
            }
        }

        public int performRegDef(bool autologin)//return 3 - server 
        {
            try
            {                
                Byte[] data = ChatClient.formatLogonMsg(this.Login, this.password);
                data[0] = autologin ? (byte)2 : (byte)1;//Registration header now
                this.stream.WriteByte(data[0]);//Identifies Registration attempt
                this.WriteWrappedEncMsg(this.stream, data);
                int resp = this.stream.ReadByte();//Ans
                switch (resp)
                {
                    case 0://Success
                        Logger.Debug(String.Format("Registration success: User '{0}' is now registered", this.Login));
                        if (!autologin)
                            this.FreeClient();
                        break;
                    case 1://Already exist
                        Logger.Debug(String.Format("Registration failed: User '{0}' already registered", this.Login));
                        this.FreeClient();
                        break;
                    default:
                        Logger.Debug("Registration failed: invalid server response");
                        return 3;
                        //break;
                }
                return resp;
            }
            catch (ArgumentNullException ex)
            {
                Logger.Error(ex.ToString);
                return 3; 
            }
            catch (SocketException ex)
            {
                Logger.Error(ex.ToString);
                return 4; 
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString); 
                return 5;
            }
        }

        public void queueChatMsg(byte type, string dest, string msg)
        {
            this.sendQueue.Enqueue(() =>
                {
                    Logger.Debug("queue");
                    Byte[] data = this.formatChatMsg(type, dest, msg);
                    this.stream.WriteByte(type);
                    this.WriteWrappedEncMsg(this.stream, data);
                });
        }

        public void requestRooms(Action ac)//type 8
        {
            this.listenQueue.Enqueue(new ListenProcessor(8, ac));
            this.stream.WriteByte(8);//Requesting rooms          
        }

        public void requestRoomUsers(string room, Action ac)//type 11
        {
            this.listenQueue.Enqueue(new ListenProcessor(11, ac));
            this.stream.WriteByte(11);//Requesting room users
            WriteWrappedMsg(this.stream, System.Text.Encoding.UTF8.GetBytes(room));
        }

        public string[] getRooms()
        {
            Byte[] bytes = ReadWrappedMsg(this.stream);
            if (bytes[0] == 0)
                return parseGetRoomsMsgAns(bytes);
            else
                throw new Exception("Invalid responce from server");
        }

        public string[] getRoomUsers()
        {
            Byte[] bytes = ReadWrappedMsg(this.stream);
            if (bytes[0] == 0)
                return ParseGetRoomUsers(bytes);
            else
                throw new Exception("Invalid responce from server");
        }

        public bool performJoinRoom(string room, string pass)//type 6
        {
            Byte[] bytes = FormatJoinRoomMsg(room, pass);
            while (this.stream.DataAvailable) { }//wait until nothing to read
            int resp = -2;
            this.mut.WaitOne();
            this.stream.WriteByte(6);//Acnowledge server about action
            this.WriteWrappedEncMsg(this.stream, bytes);
            resp = this.stream.ReadByte();
            this.mut.ReleaseMutex();
            switch (resp)
            {
                case 0:
                    // success
                    return true;
                case 1:
                    // room already exist, invalid password
                    return false;
                case 2:
                    // can't create room
                    return false;
                default:
                    throw new ChatClientException("Error joining room");
            }
        }

        // type 7
        public bool PerformLogout()
        {
            //int resp = -2;
            //while (stream.DataAvailable) { }//wait until nothing to read            
            this.mut.WaitOne();
            this.stream.WriteByte(7);
            //resp = stream.ReadByte();
            this.mut.ReleaseMutex();
            //return (resp == 0);
            return true;
        }

        // type 9
        public bool PerformLeaveRoom(string room)
        {
            this.MessageProcessor.removeProcessor(room);
            if (this.MessageProcessor.RoomCount == 0)//if user leaved all rooms
            {
                return this.PerformLogout();
            }
            else
            {
                Byte[] data = FormatLeaveRoomMsg(room);
                while (this.stream.DataAvailable) { }//wait until nothing to read
                int resp = -2;
                this.mut.WaitOne();
                this.stream.WriteByte(9);
                WriteWrappedMsg(this.stream, data);
                resp = this.stream.ReadByte();
                this.mut.ReleaseMutex();
                return resp == 0;
            }
        }

        #region Parsing

        // type - 8, message=ansver+strings count+stringsize1+data1+stringsize2+data2+...
        private static string[] parseGetRoomsMsgAns(Byte[] bytes)
        {
            //bytes[0] must be 0, if 1 then error
            string[] strs = null;
            int strCnt = BitConverter.ToInt32(bytes, 1);
            strs = new string[strCnt];
            int strBSize;
            int pos = 5;//Position in message
            for (int i = 0; i < strCnt; i++)
            {
                strBSize = BitConverter.ToInt32(bytes, pos);
                pos += 4;
                strs[i] = System.Text.Encoding.UTF8.GetString(bytes, pos, strBSize);
                pos += strBSize;
            }
            return strs;
        }

        private static string[] ParseGetRoomUsers(Byte[] bytes)
        {
            // bytes[0] must be 0, if 1 then error
            string[] strs = null;
            int strCnt = BitConverter.ToInt32(bytes, 1);
            strs = new string[strCnt];
            int strBSize;
            int pos = 5; //Position in message
            for (int i = 0; i < strCnt; i++)
            {
                strBSize = BitConverter.ToInt32(bytes, pos);
                pos += 4;
                strs[i] = System.Text.Encoding.UTF8.GetString(bytes, pos, strBSize);
                pos += strBSize;
            }

            return strs;
        }

        static void parseChatMsg(Byte[] bytes, out string source, out string dest, out string message)
        {
            // bytes[0] must be 3, 4, or 5
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

        #endregion

        #region Formatting Messages

        //----------------------------------AUTHORIZATION------------------------------------------------------------------------------------------------------
        //header = "0"+loginSize+passSize
        //data = login + pass
        private static Byte[] formatLogonMsg(string login, string pass)
        {
            int headerSize = 9;//1+4+4
            Byte[] loginB = System.Text.Encoding.UTF8.GetBytes(login);
            Byte[] passB = System.Text.Encoding.UTF8.GetBytes(pass);
            int authMessageSize = headerSize + loginB.Length + passB.Length;
            Byte[] authMessage = new Byte[authMessageSize];
            
            //Constructing header
            authMessage[0] = 0;
            BitConverter.GetBytes(loginB.Length).CopyTo(authMessage, 1);
            BitConverter.GetBytes(passB.Length).CopyTo(authMessage, 5);
            
            //Constructing data
            loginB.CopyTo(authMessage, headerSize);
            passB.CopyTo(authMessage, headerSize + loginB.Length);
            return authMessage;
        }

        /// <summary>
        /// message = header + data
        /// header = type + source(login) + room/user size(Int32) + messageSize(Int32)    
        /// </summary>
        /// <param name="msgtype">type: 3 - to room, 4 - to user, 5 - to everyone</param>
        /// <param name="dest"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private Byte[] formatChatMsg(Byte msgtype, string dest, string message)//dest sometimes is "all"
        {
            //int headerSize = 13;
            Byte[] sourceB = System.Text.Encoding.UTF8.GetBytes(this.Login);
            Byte[] destB = System.Text.Encoding.UTF8.GetBytes(dest);
            Byte[] messageB = System.Text.Encoding.UTF8.GetBytes(message);
            int dataSize = sourceB.Length + destB.Length + messageB.Length;
            
            //Header - 1+4+4+4         
            Byte[] msg = new Byte[13 + dataSize];
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

        /// <summary>
        /// Formats Join room binary message
        /// message = "6"+header+data, header=RoomNameSize+passSize, data = bRoom+bPass
        /// </summary>
        /// <param name="room"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        private static byte[] FormatJoinRoomMsg(string room, string pass)
        {
            var roomB = System.Text.Encoding.UTF8.GetBytes(room);
            var passB = System.Text.Encoding.UTF8.GetBytes(pass);
            int roomBlen = roomB.Length,
                passBlen = passB.Length;
            
            const int HeaderSize = 9; // 1+4+4
            int messageSize = HeaderSize + roomBlen + passBlen;
            var message = new Byte[messageSize];
            
            // Constructing header
            message[0] = 6;
            BitConverter.GetBytes(roomB.Length).CopyTo(message, 1);
            BitConverter.GetBytes(passB.Length).CopyTo(message, 5);
            
            // Constructing data                    
            roomB.CopyTo(message, HeaderSize);
            passB.CopyTo(message, HeaderSize + roomBlen);
            
            // Console.WriteLine("msglen {0}", message.Length);
            return message;
        }

        // msg = header(roomB length, 4 bytes) + room
        private static byte[] FormatLeaveRoomMsg(string room)
        {
            var roomB = System.Text.Encoding.UTF8.GetBytes(room);
            var msg = new byte[4 + roomB.Length]; // header + room
            BitConverter.GetBytes(roomB.Length).CopyTo(msg, 0);
            roomB.CopyTo(msg, 4);
            return msg;
        }

        #endregion

        private void InitStaticDSA()
        {
            this.staticDsaClientSigner = new ECDSAWrapper(1, true, StaticClientPrivateKey);
            this.staticDsaServerChecker = new ECDSAWrapper(1, false, StaticServerPublicKey);
        }

        #region ReadWrite wrapUnwrap

        static int ReadInt32(NetworkStream stream)
        {
            Byte[] data = new Byte[4];
            stream.Read(data, 0, data.Length);
            return BitConverter.ToInt32(data, 0);
        }

        static Byte[] ReadWrappedMsg(NetworkStream stream)
        {
            int streamDataSize = ReadInt32(stream);            
            Byte[] streamData = new Byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return streamData;
        }

        ////static int readWrappedMsg2(NetworkStream stream, ref byte[] read)
        ////{
        ////    int streamDataSize = readInt32(stream);
        ////    if (streamDataSize >= read.Length)
        ////    {
        ////        int readSZ = stream.Read(read, 0, streamDataSize);
        ////        return readSZ;
        ////    }
        ////    else throw new ArgumentException("Too small to read incoming data", "read");
        ////}

        private Byte[] ReadWrappedEncMsg(NetworkStream stream)
        {
            int streamDataSize = ReadInt32(stream);
            Byte[] streamData = new Byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return this.cryptor.Decrypt(streamData);            
        }

        static void WriteWrappedMsg(NetworkStream stream, Byte[] bytes)
        {
            Byte[] data = new Byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }

        private void WriteWrappedEncMsg(NetworkStream stream, Byte[] plain)
        {
            byte[] bytes = this.cryptor.Encrypt(plain);
            Byte[] data = new Byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }

        #endregion

        /// <summary>
        /// Generates secure random byte array of set length
        /// </summary>
        /// <param name="len">length of return byte array</param>
        /// <returns>secure random byte array of set length</returns>
        private static byte[] GenSecRandomBytes(int len)
        {
            var rand = new Org.BouncyCastle.Security.SecureRandom();
            var bytes = new byte[len];
            rand.NextBytes(bytes);
            return bytes;
        }

        private void FreeClient()
        {
            if (this.client != null)
            {
                if (this.client.Connected)
                {
                    this.client.GetStream().Close();
                }

                this.client.Close();
                this.stream = null;
                this.client = null;
            }
        }
    } 
}