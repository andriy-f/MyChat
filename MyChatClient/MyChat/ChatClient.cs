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

//----------------------------------Messages From Server------------------------------------
//type 3 4 5 - incoming message
//message header = <type>+ sourceBSize+destBSize+messageBSize+(styleSize)
//message data   = source, dest, message, (style)
//-------------------------------------------------------------------------

//Needed improvements:
//1. stream must be accessed only from listenToServer or lock stream while working with it
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace MyChat
{
    public static class ChatClient
    {
        #region Fields

        static int portNum = 13000;
        static string server = "localhost";
        static string login = "";
        static string password = "";
        static TcpClient client = null;
        static NetworkStream stream = null;
        public static MsgProcessor msgProcessor = new MsgProcessor();
        private static System.Threading.Thread listenerThread = null;

        #endregion

        #region Parameters

        public static string Server
        { get { return server; } }

        public static string Login
        { get { return login; } }

        #endregion

        #region Init & stop

        public static void init(string _serv, int _prt, string _login, string _password)
        {
            server = _serv; portNum = _prt; login = _login; password = _password;
        }        

        public static void initClient()
        {
            freeClient();
            client = new TcpClient(server, portNum);
            stream = client.GetStream();
        }

        public static void startListener()
        {
            if (listenerThread != null) stopListener();
            listenerThread = new System.Threading.Thread(new System.Threading.ThreadStart(listenToServer));
            listenerThread.Priority = System.Threading.ThreadPriority.Lowest;
            listenerThread.Start();
        }

        public static void stopListener()
        { listenerThread.Abort(); }

        public static void freeClient()
        { if (client != null) { client.GetStream().Close(); client.Close(); client = null; stream = null; } }

        #endregion

        #region listenToServer

        public static void listenToServer()
        {
            try
            {
                //throw new IndexOutOfRangeException(); find way to report to GUI
                while (true)
                    if (msgProcessor.RoomCount > 0 && stream.DataAvailable)
                        lock (stream)
                        {
                            Byte[] streamData;// = readWrappedMsg(stream);
                            int type = stream.ReadByte();
                            string source, dest, message;
                            switch (type)
                            {
                                case 1://
                                    msgProcessor.process("Server", "<unknown>", "Previous message was not deivered");
                                    break;
                                case 3://Incoming Message for room                                    
                                    streamData = readWrappedMsg(stream);//streamData[0] must == 3
                                    parseChatMsg(streamData, out source, out dest, out message);
                                    //displaying Message
                                    Program.LogEvent(String.Format("[{0}] to [{1}]: \"{2}\"", source, dest, message));
                                    msgProcessor.processForRoom(source, dest, message);
                                    break;
                                case 4://Incoming Message for user
                                    streamData = readWrappedMsg(stream);//streamData[0] must == 4
                                    parseChatMsg(streamData, out source, out dest, out message);
                                    //displaying Message
                                    Program.LogEvent(String.Format("[{0}] to [{1}]: \"{2}\"", source, dest, message));
                                    msgProcessor.process(source, dest, message);
                                    break;
                                case 5://Incoming Message for All
                                    streamData = readWrappedMsg(stream);//streamData[0] must == 5
                                    parseChatMsg(streamData, out source, out dest, out message);
                                    //displaying Message
                                    Program.LogEvent(String.Format("[{0}] to [{1}]: \"{2}\"", source, dest, message));
                                    msgProcessor.process(source, dest, message);
                                    break;
                                default:
                                    Program.LogEvent("Server sent unknown message");
                                    break;
                            }
                        }

            }
            catch (System.Threading.ThreadAbortException)
            {
                Program.LogEvent("Listener stopped by aborting (normal)");
            }
            catch (Exception ex)
            {
                Program.LogException(new Exception("Error while listening to server", ex));
                //Program.LogException(ex);
            }
        }

        #endregion

        #region Perform

        public static int performLoginDef()
        {
            try
            {
                initClient();
                Byte[] data = ChatClient.formatAuthMsg(login, password);
                stream.WriteByte(0);//Identifies login attempt
                writeWrappedMsg(stream, data);
                int resp = stream.ReadByte();//Ans
                switch (resp)
                {
                    case 0://Success
                        Program.LogEvent(String.Format("Logged successfully with login '{0}'", login));
                        startListener();
                        break;
                    case 1://Already logged on
                        Program.LogEvent(String.Format("Logon fail: User '{0}' already logged on", login));
                        freeClient();
                        break;
                    case 2://Invalid login/pass
                        Program.LogEvent(String.Format("Logon fail: Invalid login//pass"));
                        freeClient();
                        break;
                }
                return resp;
            }
            catch (ArgumentNullException ex)
            {
                Program.LogException(new Exception("ArgumentNullException mod", ex));                
                return 3;
            }
            catch (SocketException ex)
            {
                Program.LogException(new Exception("SocketException: {0}", ex));
                return 4;
            }
        }

        public static int performRegDef(bool autologin)//return 3 - server 
        {
            try
            {
                initClient();
                Byte[] data = ChatClient.formatAuthMsg(login, password);
                data[0] = autologin ? (byte)2 : (byte)1;//Registration header now
                stream.WriteByte(data[0]);//Identifies Registration attempt
                writeWrappedMsg(stream, data);
                int resp = stream.ReadByte();//Ans
                switch (resp)
                {
                    case 0://Success
                        Program.LogEvent(String.Format("Registration success: User '{0}' is now registered", login));
                        if (!autologin)
                            freeClient();
                        break;
                    case 1://Already exist
                        Program.LogEvent(String.Format("Registration failed: User '{0}' already registered", login));
                        freeClient();
                        break;                    
                    default:
                        Program.LogEvent("Registration failed: invalid server response");
                        return 3;
                        //break;
                }
                return resp;
            }
            catch (ArgumentNullException ex)
            { Console.Error.WriteLine("ArgumentNullException: {0}", ex); return 3; }
            catch (SocketException ex)
            { Console.Error.WriteLine("SocketException: {0}", ex); return 4; }
        }

        public static void performChatMsg(byte type, string dest, string msg)
        {
            Byte[] data = formatChatMsg(type, dest, msg);
            while (stream.DataAvailable) { }//wait until nothing to read
            listenerThread.Suspend();
            stream.WriteByte(type);
            writeWrappedMsg(stream, data);
            //return (stream.DataAvailable && stream.ReadByte() == 1);
            listenerThread.Resume();
        }

        public static string[] performGetRooms()//type 8
        {
            while (stream.DataAvailable) { }//wait until nothing to read
            listenerThread.Suspend();
            stream.WriteByte(8);//Queriing server for rooms
            Byte[] bytes = readWrappedMsg(stream);
            listenerThread.Resume();
            if (bytes[0] == 0)
                return parseGetRoomsMsgAns(bytes);
            else return null;//error
        }

        public static bool performJoinRoom(string room, string pass)//type 6
        {
            Byte[] bytes = formatJoinRoomMsg(room, pass);
            while (stream.DataAvailable) { }//wait until nothing to read
            int resp = -2;
            lock (stream)
            {
                stream.WriteByte(6);//Acnowledge server about action
                writeWrappedMsg(stream, bytes);
                resp = stream.ReadByte();
            }
            switch (resp)
            {
                case 0://success
                    return true;
                //break;
                case 1://room already exist, invalid password
                    return false;
                //break;
                case 2://can't create room
                    return false;
                //break;
                default:                    
                    throw new ChatClientException("Invalid query return value");
            }
        }

        public static bool performLogout()//type 7
        {
            while (stream.DataAvailable) { }//wait until nothing to read
            int resp = -2;
            lock (stream)
            {
                stream.WriteByte(7);
                resp = stream.ReadByte();
            }
            return (resp == 0);
        }

        public static bool performLeaveRoom(string room)//type 9
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
                lock (stream)
                {
                    stream.WriteByte(9);
                    writeWrappedMsg(stream, data);
                    resp=stream.ReadByte();
                }
                return (resp  == 0);
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
        private static Byte[] formatAuthMsg(string login, string pass)
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

        private static Byte[] formatChatMsg(Byte msgtype, string dest, string message)//dest sometimes is "all"
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

        private static Byte[] formatChatMsgSign(Byte msgtype, string dest, string message)//dest sometimes is "all"
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

        public static void testSign()
        {
            //Create a new instance of DSACryptoServiceProvider to generate
            //a new key pair.
            DSACryptoServiceProvider DSA = new DSACryptoServiceProvider();

            //The hash value to sign.
            byte[] HashValue = { 59, 4, 248, 102, 77, 97, 142, 201, 210, 12, 224, 93, 25, 41, 100, 197, 213, 134, 130, 135 };

            //The value to hold the signed value.
            byte[] SignedHashValue = mySignHash(HashValue, DSA.ExportParameters(true), "SHA1");

            //Verify the hash and display the results.
            if (myVerifyHash(HashValue, SignedHashValue, DSA.ExportParameters(false), "SHA1"))
            {
                Console.WriteLine("The hash value was verified.");
            }
            else
            {
                Console.WriteLine("The hash value was not verified.");
            }
        }


        public static byte[] mySignHash(byte[] HashToSign, DSAParameters DSAKeyInfo, string HashAlg)
        {
            try
            {
                //Create a new instance of DSACryptoServiceProvider.
                DSACryptoServiceProvider DSA = new DSACryptoServiceProvider();
                //Import the key information.   
                DSA.ImportParameters(DSAKeyInfo);
                //Create an DSASignatureFormatter object and pass it the 
                //DSACryptoServiceProvider to transfer the private key.
                DSASignatureFormatter DSAFormatter = new DSASignatureFormatter(DSA);
                //Set the hash algorithm to the passed value.
                DSAFormatter.SetHashAlgorithm(HashAlg);
                //Create a signature for HashValue and return it.
                return DSAFormatter.CreateSignature(HashToSign);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public static bool myVerifyHash(byte[] HashValue, byte[] SignedHashValue, DSAParameters DSAKeyInfo, string HashAlg)
        {
            try
            {
                //Create a new instance of DSACryptoServiceProvider.
                DSACryptoServiceProvider DSA = new DSACryptoServiceProvider();
                //Import the key information. 
                DSA.ImportParameters(DSAKeyInfo);
                //DSAKeyInfo.
                //Create an DSASignatureDeformatter object and pass it the 
                //DSACryptoServiceProvider to transfer the private key.
                DSASignatureDeformatter DSADeformatter = new DSASignatureDeformatter(DSA);
                //Set the hash algorithm to the passed value.
                DSADeformatter.SetHashAlgorithm(HashAlg);
                //Verify signature and return the result. 
                return DSADeformatter.VerifySignature(HashValue, SignedHashValue);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

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

        static void writeWrappedMsg(NetworkStream stream, Byte[] bytes)
        {
            Byte[] data = new Byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
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

    public class MsgProcessor
    {
        public delegate void ReceiveMsgProcessor(string source, string dest, string msg);

        class RoomParams
        {
            public ReceiveMsgProcessor processor;

            public RoomParams(ReceiveMsgProcessor prc)
            { processor = prc; }
        }

        private Hashtable roomProcessors = new Hashtable(5);//roomName/RoomParams

        public int RoomCount
        { get { return roomProcessors.Count; } }

        public bool addProcessor(string room, ReceiveMsgProcessor proc)//When user joins room
        {
            if (!roomProcessors.Contains(room))
            {
                roomProcessors.Add(room, new RoomParams(proc));
                return true;
            }
            else return false;
        }

        public bool removeProcessor(string room)//When user leaves room
        {
            if (roomProcessors.Contains(room))
            {
                roomProcessors.Remove(room);
                return true;
            }
            else return false;
        }

        public void process(string source, string dest, string message)//For user or All
        {
            foreach (System.Collections.DictionaryEntry de in roomProcessors)
                ((RoomParams)de.Value).processor(source, dest, message);
        }

        public bool processForRoom(string source, string dest, string message)//room==dest
        {
            if (roomProcessors.Contains(dest))
            {
                ((RoomParams)roomProcessors[dest]).processor(source, dest, message);
                return true;
            }
            else return false;
        }
    }
}