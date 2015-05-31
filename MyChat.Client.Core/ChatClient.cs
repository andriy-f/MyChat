//ChatClient.cs - for handling and sending requests to server
//--------------------------------------------------------------

//--------------------------------Queries to server-------------------------------------------
//type - in 1st byte
//type 0 - authorization(login) answers: 0-success, 1-already logged on, 2-invalid login/pass
//type 1 - registration ans: 0-success, 1-already exist, 2 - registration unaviable
//type 2 - type 1 + autologin

//type 3,4,5 - chat message 
//type 6 - join room - if room !exist => create with pass, else check pass
//type 7 - logout
//type 8 - get rooms
//type 9 - leave room  
//type 10 - check if alive
//typr 11 - get room users

//----------------------------------Messages From Server------------------------------------
//type 3 4 5 - incoming message
//message header = <type>+ sourceBSize+destBSize+messageBSize+(styleSize)
//message data   = source, dest, message, (style)
//-------------------------------------------------------------------------

//Needed improvements:
//1. stream must be accessed only from listenToServer or lock stream while working with it

namespace Andriy.MyChat.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;

    using Andriy.Security.Cryptography;

    using global::MyChat.Client.Core.Logging;

    using ECDHWrapper = Andriy.Security.Cryptography.ECDHWrapper;
    using ECDSAWrapper = Andriy.Security.Cryptography.ECDSAWrapper;

    public class ChatClient
    {
        #region Fields

        const int agrlen = 32;

        private static readonly ILog Logger = LogProvider.GetLogger(typeof(ChatClient));

        int portNum = 13000;

        string server = "localhost";

        string login = "";

        string password = "";
        
        private TcpClient client;
        private NetworkStream stream;

        public MsgProcessor msgProcessor = new MsgProcessor();

        internal static Queue<ListenProcessor> listenQueue = new Queue<ListenProcessor>();

        internal static Queue<Action> sendQueue = new Queue<Action>();

        private System.Threading.Thread listenerThread;
        private System.Threading.Mutex mut = new System.Threading.Mutex();

        public static readonly byte[] staticServerPubKey = { 4, 81, 97, 253, 33, 148, 211, 27, 164, 103, 98, 244, 190, 246, 165, 216, 112, 148, 56, 28, 38, 55, 92, 241, 130, 210, 62, 81, 127, 210, 78, 3, 95, 35, 72, 221, 34, 5, 200, 194, 215, 102, 191, 60, 52, 30, 164, 242, 52, 255, 64, 199, 132, 23, 249, 234, 50, 171, 242, 160, 223 };
        public static readonly byte[] staticClientPrivKey = { 57, 16, 61, 194, 88, 158, 65, 114, 36, 14, 242, 62, 215, 205, 157, 122, 229, 105, 130, 118, 235, 214, 214, 25, 171, 106, 38, 200, 35, 185 };
        
        static byte[] iv1 = { 111, 62, 131, 223, 199, 122, 219, 32, 13, 147, 249, 67, 137, 161, 97, 104 };

        static ECDSAWrapper staticDsaClientSigner;//signs with staticClientPrivKey
        static ECDSAWrapper staticDsaServerChecker;//check with staticServerPubKey 

        static AESCSPImpl cryptor;

        //static ECDSAWrapper seanceDsaClientSigner;//signs client's messages
        //static ECDSAWrapper seanceDsaServerChecker;//checks servers messages

        #endregion

        #region Parameters

        public string Server
        {
            get
            {
                return server;
            }
        }

        public string Login
        {
            get
            {
                return login;
            }
        }

        #endregion

        #region Init & stop

        public void init(string _serv, int _prt, string _login, string _password)
        {
            server = _serv; 
            portNum = _prt; 
            login = _login; 
            password = _password;
            initStaticDSA();
            initClient();
        }        

        public void initClient()
        {
            freeClient();
            client = new TcpClient(server, portNum);            
            stream = client.GetStream();
            stream.ReadTimeout = 7000;
        }

        public void startListener()
        {
            if (listenerThread != null)
                stopListener();
            listenerThread = new System.Threading.Thread(new System.Threading.ThreadStart(listenToServer));
            listenerThread.Priority = System.Threading.ThreadPriority.Lowest;
            listenerThread.Start();
            Logger.Info("Listening started");
        }

        public void stopListener()
        {
            listenerThread.Abort();
        }

        public void freeClient()
        {
            if (client != null)
            {
                if(client.Connected)
                    client.GetStream().Close();
                client.Close();
                stream = null; 
                client = null;                
            }
        }

        #endregion

        #region listenToServer

        public void listenToServer()
        {
            try
            {
                //throw new IndexOutOfRangeException(); find way to report to GUI
                while (true)
                {
                    if (sendQueue.Count > 0)
                    {
                        Action toSend = sendQueue.Dequeue();
                        toSend();
                    }
                    if (stream.DataAvailable)
                    {
                        mut.WaitOne();
                        Byte[] streamData;// = readWrappedMsg(stream);
                        int type = stream.ReadByte();
                        string source, dest, message;
                        switch (type)
                        {
                            case 1:                                
                                msgProcessor.process("Server", "<unknown>", "Previous message was not deivered");
                                break;
                            case 3://Incoming Message for room                                    
                                streamData = readWrappedEncMsg(stream);//streamData[0] must == 3
                                parseChatMsg(streamData, out source, out dest, out message);
                                //displaying Message
                                Logger.Info(string.Format("[{0}] -> [{1}]: \"{2}\"", source, dest, message));
                                msgProcessor.processForRoom(source, dest, message);
                                break;
                            case 4://Incoming Message for user
                                streamData = readWrappedEncMsg(stream);//streamData[0] must == 4
                                parseChatMsg(streamData, out source, out dest, out message);
                                //displaying Message
                                Logger.Info(String.Format("[{0}] -> [{1}]: \"{2}\"", source, dest, message));
                                msgProcessor.process(source, dest, message);
                                break;
                            case 5://Incoming Message for All
                                streamData = readWrappedEncMsg(stream);//streamData[0] must == 5
                                parseChatMsg(streamData, out source, out dest, out message);
                                //displaying Message
                                Logger.Info(String.Format("[{0}] -> [{1}]: \"{2}\"", source, dest, message));
                                msgProcessor.process(source, dest, message);
                                break;
                            case 10:
                                stream.WriteByte(10);//I'm alive!
                                break;
                            default:
                                if (listenQueue.Count > 0 && type == listenQueue.Peek().code)
                                {
                                    listenQueue.Dequeue().toDo();
                                    break;
                                }
                                
                                throw new Exception("Server sent unknown token");
                        }

                        mut.ReleaseMutex();
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

        #endregion

        #region Perform

        /// <summary>
        /// Performs authentification
        /// </summary>
        /// <returns>0 if OK, 1 if wrong, 2 if exception</returns>
        public int performAuth()
        {
            try
            {
                //server sends random data which client must sign
                byte[] rec = readWrappedMsg(stream);
                byte[] send = staticDsaClientSigner.signHash(rec);
                writeWrappedMsg(stream, send);
                //now client checks that server is legit
                //client gen random data which server must sign
                send = genSecRandomBytes(100);
                writeWrappedMsg(stream, send);
                rec = readWrappedMsg(stream);
                if (staticDsaServerChecker.verifyHash(send, rec))
                    return 0;
                else 
                    return 1;
            }
            catch (Exception ex)
            {
                Logger.Debug(String.Format("Error while authentificating: {0}{1}", Environment.NewLine, ex));
                return 2;
            }
        }

        public bool performAgreement()
        {
            try
            {
                ECDHWrapper ecdh1 = new ECDHWrapper(agrlen);
                writeWrappedMsg(stream, ecdh1.PubData);                
                byte[] recSerPub = readWrappedMsg(stream);
                byte[] agr = ecdh1.calcAgreement(recSerPub);

                int aeskeylen=32;
                byte[] aeskey=new byte[aeskeylen];
                Array.Copy(agr, 0, aeskey, 0, aeskeylen);

                cryptor = new AESCSPImpl(aeskey, iv1);                
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
                Byte[] data = ChatClient.formatLogonMsg(login, password);
                stream.WriteByte(0);//Identifies logon attempt
                writeWrappedEncMsg(stream, data);
                int resp = stream.ReadByte();//Ans
                switch (resp)
                {
                    case 0://Success
                        Logger.Debug(String.Format("Logon success with login '{0}'", login));                        
                        break;
                    case 1://Already logged on
                        Logger.Debug(String.Format("Logon fail: User '{0}' already logged on", login));
                        freeClient();
                        break;
                    case 2://Invalid login/pass
                        Logger.Debug(String.Format("Logon fail: Invalid login//pass"));
                        freeClient();
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
                Byte[] data = ChatClient.formatLogonMsg(login, password);
                data[0] = autologin ? (byte)2 : (byte)1;//Registration header now
                stream.WriteByte(data[0]);//Identifies Registration attempt
                writeWrappedEncMsg(stream, data);
                int resp = stream.ReadByte();//Ans
                switch (resp)
                {
                    case 0://Success
                        Logger.Debug(String.Format("Registration success: User '{0}' is now registered", login));
                        if (!autologin)
                            freeClient();
                        break;
                    case 1://Already exist
                        Logger.Debug(String.Format("Registration failed: User '{0}' already registered", login));
                        freeClient();
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
            sendQueue.Enqueue(() =>
                {
                    Logger.Debug("queue");
                    Byte[] data = formatChatMsg(type, dest, msg);
                    stream.WriteByte(type);
                    writeWrappedEncMsg(stream, data);
                });
        }

        public void requestRooms(Action ac)//type 8
        {
            listenQueue.Enqueue(new ListenProcessor(8, ac));
            stream.WriteByte(8);//Requesting rooms          
        }

        public void requestRoomUsers(string room, Action ac)//type 11
        {
            listenQueue.Enqueue(new ListenProcessor(11, ac));
            stream.WriteByte(11);//Requesting room users
            writeWrappedMsg(stream, System.Text.Encoding.UTF8.GetBytes(room));
        }

        public string[] getRooms()
        {
            Byte[] bytes = readWrappedMsg(stream);
            if (bytes[0] == 0)
                return parseGetRoomsMsgAns(bytes);
            else
                throw new Exception("Invalid responce from server");
        }

        public string[] getRoomUsers()
        {
            Byte[] bytes = readWrappedMsg(stream);
            if (bytes[0] == 0)
                return parseGetRoomUsers(bytes);
            else
                throw new Exception("Invalid responce from server");
        }

        public bool performJoinRoom(string room, string pass)//type 6
        {
            Byte[] bytes = formatJoinRoomMsg(room, pass);
            while (stream.DataAvailable) { }//wait until nothing to read
            int resp = -2;
            mut.WaitOne();
            stream.WriteByte(6);//Acnowledge server about action
            writeWrappedEncMsg(stream, bytes);
            resp = stream.ReadByte();
            mut.ReleaseMutex();
            switch (resp)
            {
                case 0://success
                    return true;
                case 1://room already exist, invalid password
                    return false;
                case 2://can't create room
                    return false;
                default:
                    throw new ChatClientException("Error joining room");
            }
        }

        public bool performLogout()//type 7
        {
            //int resp = -2;
            //while (stream.DataAvailable) { }//wait until nothing to read            
            mut.WaitOne();
            stream.WriteByte(7);
            //resp = stream.ReadByte();
            mut.ReleaseMutex();
            //return (resp == 0);
            return true;
        }

        public bool performLeaveRoom(string room)//type 9
        {
            msgProcessor.removeProcessor(room);
            if (msgProcessor.RoomCount == 0)//if user leaved all rooms
            {
                return performLogout();
            }
            else
            {
                Byte[] data = formatLeaveRoomMsg(room);
                while (stream.DataAvailable) { }//wait until nothing to read
                int resp = -2;
                mut.WaitOne();
                stream.WriteByte(9);
                writeWrappedMsg(stream, data);
                resp = stream.ReadByte();
                mut.ReleaseMutex();
                return (resp == 0);
            }
        }

        #endregion

        #region Parsing

        //type - 8, message=ansver+strings count+stringsize1+data1+stringsize2+data2+...
        static string[] parseGetRoomsMsgAns(Byte[] bytes)
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

        static string[] parseGetRoomUsers(Byte[] bytes)
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

        static void parseChatMsg(Byte[] bytes, out string source, out string dest, out string message)
        {
            //bytes[0] must be 3, 4, or 5
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

        //---------------------------------------MESSAGE------------------------------------------------------------------------------------------
        //message = header + data
        //type: 3 - to room, 4 - to user, 5 - to everyone
        //
        //header = type + source(login) + room/user size(Int32) + messageSize(Int32)        

        private Byte[] formatChatMsg(Byte msgtype, string dest, string message)//dest sometimes is "all"
        {
            //int headerSize = 13;
            Byte[] sourceB = System.Text.Encoding.UTF8.GetBytes(login);
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

        private Byte[] formatChatMsgSign(Byte msgtype, string dest, string message)//dest sometimes is "all"
        {
            //int headerSize = 13;
            Byte[] sourceB = System.Text.Encoding.UTF8.GetBytes(login);
            Byte[] destB = System.Text.Encoding.UTF8.GetBytes(dest);
            Byte[] messageB = System.Text.Encoding.UTF8.GetBytes(message);
            //Byte[] 
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

        private static Byte[] signData(Byte[] data )
        {
            //Byte[] 
            int dataSize = data.Length;
            //Header - 1+4+4+4 
            Byte[] sdata = new Byte[13 + dataSize];
            
            
           
            return sdata;
        }

        //------------------------------JOIN ROOM--------------------------------------------------
        //message = "6"+header+data, header=RoomNameSize+passSize, data = bRoom+bPass
        private static Byte[] formatJoinRoomMsg(string room, string pass)
        {
            Byte[] roomB = System.Text.Encoding.UTF8.GetBytes(room);
            Byte[] passB = System.Text.Encoding.UTF8.GetBytes(pass);
            int roomBlen = roomB.Length,
                passBlen = passB.Length;
            int headerSize = 9;//1+4+4
            int messageSize = headerSize + roomBlen + passBlen;
            Byte[] message = new Byte[messageSize];
            //Constructing header
            message[0] = 6;
            BitConverter.GetBytes(roomB.Length).CopyTo(message, 1);
            BitConverter.GetBytes(passB.Length).CopyTo(message, 5);
            //Constructing data                    
            roomB.CopyTo(message, headerSize);
            passB.CopyTo(message, headerSize + roomBlen);
            //Console.WriteLine("msglen {0}", message.Length);
            return message;
        }

        //msg = header(roomB length, 4 bytes) + room
        private static Byte[] formatLeaveRoomMsg(string room)
        {
            Byte[] roomB = System.Text.Encoding.UTF8.GetBytes(room);
            Byte[] msg = new Byte[4 + roomB.Length];//header + room
            BitConverter.GetBytes(roomB.Length).CopyTo(msg, 0);
            roomB.CopyTo(msg, 4);
            return msg;
        }

        #endregion

        #region Signing

        private static void initStaticDSA()
        {
            staticDsaClientSigner = new ECDSAWrapper(1, true, staticClientPrivKey);
            staticDsaServerChecker = new ECDSAWrapper(1, false, staticServerPubKey);
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
                int readSZ=stream.Read(read, 0, streamDataSize);
                return readSZ;
            }
            else throw new ArgumentException("Too small to read incoming data", "read");
        }

        static Byte[] readWrappedEncMsg(NetworkStream stream)
        {
            int streamDataSize = readInt32(stream);
            Byte[] streamData = new Byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return cryptor.Decrypt(streamData);            
        }

        static void writeWrappedMsg(NetworkStream stream, Byte[] bytes)
        {
            Byte[] data = new Byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }

        static void writeWrappedEncMsg(NetworkStream stream, Byte[] plain)
        {
            byte[] bytes = cryptor.Encrypt(plain);
            Byte[] data = new Byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
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

        #region OBSOLETE

        /*//header = "type"+header+data, header=size of data, data = room
        public static Byte[] formatUpdateMsgOld(string room)
        {
            Byte[] roomB = System.Text.Encoding.UTF8.GetBytes(room);
            int headerSize = 5;//1+4
            int messageSize = headerSize + roomB.Length;
            Byte[] message = new Byte[messageSize];
            //Constructing header
            message[0] = 6;
            BitConverter.GetBytes(roomB.Length).CopyTo(message, 1);
            //Constructing data                    
            roomB.CopyTo(message, headerSize);
            return message;
        }
		*/

        /*//message = header + data
        //type: 3 - to room, 4 - to user, 5 - to everyone
        //
        //if type = 3 or 4 --> header = type + room/user size(Int32) + messageSize(Int32)
        //else header = type + messageSize

        public static Byte[] formatChatMsgOld(Byte type, string dest, string messageText)
        {
            if (type == 3 || type == 4)
            {
                Byte[] destB = System.Text.Encoding.UTF8.GetBytes(dest);
                Byte[] messageTextB = System.Text.Encoding.UTF8.GetBytes(messageText);
                int headerSize = 9;//1+4+4
                int chatMessageSize = headerSize + destB.Length + messageTextB.Length;
                Byte[] chatMessage = new Byte[chatMessageSize];
                //Constructing header
                chatMessage[0] = type;
                BitConverter.GetBytes(destB.Length).CopyTo(chatMessage, 1);
                BitConverter.GetBytes(messageTextB.Length).CopyTo(chatMessage, 5);
                //Constructing data
                destB.CopyTo(chatMessage, headerSize);
                messageTextB.CopyTo(chatMessage, headerSize + destB.Length);
                return chatMessage;
            }
            else if (type == 5)
            {
                Byte[] messageTextB = System.Text.Encoding.UTF8.GetBytes(messageText);
                int headerSize = 5;//1+4
                int chatMessageSize = headerSize + messageTextB.Length;
                Byte[] chatMessage = new Byte[chatMessageSize];
                //Constructing header
                chatMessage[0] = type;
                BitConverter.GetBytes(messageTextB.Length).CopyTo(chatMessage, 1);
                //Constructing data                    
                messageTextB.CopyTo(chatMessage, headerSize);
                return chatMessage;
            }
            else return null;
        }
        */

        /*static void readIncomingMsg(NetworkStream stream, out string source, out string dest, out string message)
        {
            //Reading header
            Byte[] bytes = new Byte[12];//4+4+4
            stream.Read(bytes, 0, bytes.Length);
            int sourceBSize = BitConverter.ToInt32(bytes, 0);
            int destBSize = BitConverter.ToInt32(bytes, 4);
            int messageBSize = BitConverter.ToInt32(bytes, 8);
            //Reading data
            int dataSize=sourceBSize+destBSize+messageBSize;
            bytes = new Byte[dataSize];
            stream.Read(bytes, 0, bytes.Length);
            source = System.Text.Encoding.UTF8.GetString(bytes,0,sourceBSize);
            dest = System.Text.Encoding.UTF8.GetString(bytes, sourceBSize, destBSize);
            message = System.Text.Encoding.UTF8.GetString(bytes, sourceBSize+destBSize, messageBSize);
        }*/

        //type - 8, message=ansver+strings count+stringsize1+data1+stringsize2+data2+...
        /*public static string[] readGetRoomsMsg()
        {
            NetworkStream stream = client.GetStream();
            stream.WriteByte(8);
            if (stream.DataAvailable)
            {
                Byte[] data = new Byte[1];
                stream.Read(data, 0, 1);
                Byte resp = data[0];
                string[] strs = null;
                switch (resp)
                {
                    case 0://Success
                        data = new Byte[4];
                        stream.Read(data, 0, data.Length);
                        int strCnt = BitConverter.ToInt32(data, 0);
                        strs = new string[strCnt];

                        Byte[] strB; int strBSize;
                        for (int i = 0; i < strCnt; i++)
                        {
                            stream.Read(data, 0, data.Length);
                            strBSize = BitConverter.ToInt32(data, 0);
                            strB = new Byte[strBSize];
                            stream.Read(strB, 0, strBSize);
                            strs[i] = System.Text.Encoding.UTF8.GetString(strB);
                        }
                        break;
                    default://Error
                        Console.WriteLine("Invalid GetRooms message"); break;
                }
                return strs;
            }
            else return null;
        }*/

        #endregion
    } 
}