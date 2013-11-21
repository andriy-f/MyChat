namespace Andriy.MyChat.Server
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;

    using Andriy.Security.Cryptography;

    /// <summary>
    /// client == null -> client incative
    /// </summary>
    public class ChatClient
    {
        private const int AgreementLength = 32;

        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ChatClient));

        /// <summary>
        /// Must be list of unique
        /// </summary>
        private readonly List<string> rooms = new List<string>(3);

        public ChatClient(TcpClient client)
        {
            this.Tcp = client;
        }

        public enum Status
        {
            Uninitialized,
            Validated,
            Encrypted,
            LoggedOn
        }
        
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
        
        /// <summary>
        /// TODO: use this
        /// </summary>
        ////public void ProcessCurrentConnection()
        ////{
        ////    byte[] bytes;
        ////    System.Net.IPAddress ipAddress = Utils.TCPClient2IPAddress(client);
        ////    Program.LogEvent(string.Format("Connected from {0}", ipAddress));
        ////    NetworkStream stream = AtcpClient.GetStream();
        ////    stream.ReadTimeout = 1000;
        ////    try
        ////    {
        ////        int authatt = processAuth(stream);
        ////        if (authatt == 0)
        ////        {
        ////            AESCSPImpl cryptor;
        ////            if (processAgreement(stream, out cryptor) == 0)
        ////            {
        ////                Byte type = (byte)stream.ReadByte();
        ////                string login, pass;
        ////                switch (type)
        ////                {
        ////                    case 0:
        ////                        //Logon attempt
        ////                        bytes = readWrappedEncMsg(stream, cryptor);
        ////                        parseLogonMsg(bytes, out login, out pass);
        ////                        if (dataGetter.ValidateLoginPass(login, pass))
        ////                            if (isLogged(login))
        ////                            {
        ////                                ChatClient oldUP = (ChatClient)clientBase[login];
        ////                                int oldresp = -2;
        ////                                if (oldUP.client.Connected)
        ////                                {
        ////                                    NetworkStream oldStream = oldUP.client.GetStream();

        ////                                    try
        ////                                    {
        ////                                        oldStream.WriteByte(10);
        ////                                        oldresp = oldStream.ReadByte();
        ////                                    }
        ////                                    catch (System.IO.IOException)
        ////                                    {
        ////                                        //Timeout - old client probably dead
        ////                                    }
        ////                                }

        ////                                if (oldresp == 10)
        ////                                {
        ////                                    //Client with login <login> still alive -> new login attempt invalid
        ////                                    stream.WriteByte(1);
        ////                                    freeTCPClient(client);
        ////                                    Program.LogEvent(string.Format("Logon from IP '{0}' failed: User '{1}' already logged on", ipAddress, login));
        ////                                }
        ////                                else
        ////                                {
        ////                                    //old client with login <login> dead -> dispose of him and connect new
        ////                                    freeTCPClient(oldUP.client);
        ////                                    removeClient(login);
        ////                                    processAndAcceptNewClient(client, login, cryptor);
        ////                                    Program.LogEvent(string.Format("Logon from IP '{0}' success: User '{1}' from IP  logged on (old client disposed)", ipAddress, login));
        ////                                }
        ////                            }
        ////                            else
        ////                            {
        ////                                processAndAcceptNewClient(client, login, cryptor);
        ////                                Program.LogEvent(string.Format("Logon from IP '{0}' success: User '{1}' from IP  logged on", ipAddress, login));
        ////                            }
        ////                        else
        ////                        {
        ////                            stream.WriteByte(2);
        ////                            freeTCPClient(client);
        ////                            Program.LogEvent(string.Format("Logon from IP '{0}' failed: Login '{1}'//Password not recognized", ipAddress, login));
        ////                        }
        ////                        break;
        ////                    case 1:
        ////                        //Registration without logon
        ////                        bytes = readWrappedEncMsg(stream, cryptor);
        ////                        parseLogonMsg(bytes, out login, out pass);
        ////                        if (!dataGetter.ValidateLogin(login))
        ////                        {
        ////                            dataGetter.AddNewLoginPass(login, pass);
        ////                            stream.WriteByte(0);
        ////                            Program.LogEvent(string.Format("Registration success: User '{0}' registered", login));
        ////                        }
        ////                        else
        ////                        {
        ////                            stream.WriteByte(1);
        ////                            Program.LogEvent(string.Format("Registration failed: User '{0}' already registered", login));
        ////                        }
        ////                        freeTCPClient(client);
        ////                        break;
        ////                    default:
        ////                        //Wrong data received
        ////                        throw new Exception();
        ////                }
        ////            }
        ////        }
        ////        else if (authatt == 1)
        ////        {
        ////            freeTCPClient(client);
        ////            Program.LogEvent(string.Format("Auth from IP '{0}' fail because client is not legit", ipAddress));
        ////            //Ban IP...
        ////        }
        ////        else
        ////        {
        ////            freeTCPClient(client);
        ////            Program.LogEvent(string.Format("Auth from IP '{0}' fail because error. See previous message for details", ipAddress));
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        Program.LogException(new Exception(String.Format("New connetion from IP {0} failed",
        ////            ipAddress), ex));
        ////        freeTCPClient(client);
        ////        //Ban IP ipAddress...
        ////    }
        ////}

        /// <summary>
        /// Check if client [application] is original, 
        /// i.e. if it has valid private key
        /// </summary> 
        /// <returns>0 if ok, 1 if wrong</returns>
        internal int Verify()
        {
            try
            {
                var stream = this.Tcp.GetStream();
                
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
        /// Sets up secure channel (this.Cryptor)
        /// </summary>
        /// <returns></returns>
        public int SetUpSecureChannel()
        {
            try
            {
                var ecdh1 = new ECDHWrapper(AgreementLength);
                var stream = this.Tcp.GetStream();
                var recCliPub = this.ReadWrappedMsg();
                WriteWrappedMsg(stream, ecdh1.PubData);
                var agr = ecdh1.calcAgreement(recCliPub);

                const int AESKeyLength = 32;
                var aeskey = new byte[AESKeyLength];
                Array.Copy(agr, 0, aeskey, 0, AESKeyLength);

                this.Cryptor = new AESCSPImpl(aeskey, ChatServer.CryptoIv1);
                return 0;
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error while completing agreement: {0}{1}", Environment.NewLine, ex);
                this.Cryptor = null;
                return 1;
            }
        }

        private static int ReadInt32(NetworkStream stream)
        {
            var data = new byte[4];
            stream.Read(data, 0, data.Length);
            return BitConverter.ToInt32(data, 0);
        }

        public byte[] ReadWrappedMsg()
        {
            var stream = this.Tcp.GetStream();
            int streamDataSize = ReadInt32(stream);
            var streamData = new byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return streamData;
        }

        public byte[] ReadWrappedEncMsg()
        {
            var stream = this.Tcp.GetStream();
            int streamDataSize = ReadInt32(stream);
            var streamData = new byte[streamDataSize];
            stream.Read(streamData, 0, streamDataSize);
            return this.Cryptor.Decrypt(streamData);
        }

        public static void WriteWrappedMsg(System.IO.Stream stream, byte[] bytes)
        {
            var data = new byte[4 + bytes.Length];
            BitConverter.GetBytes(bytes.Length).CopyTo(data, 0);
            bytes.CopyTo(data, 4);
            stream.Write(data, 0, data.Length);
        }
    }
}