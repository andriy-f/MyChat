namespace Andriy.MyChat.Server
{
    using System;
    using System.Windows.Forms;

    using Andriy.Security.Cryptography;

    public static class Program
    {
        #region Fields

        static readonly byte[] SettKey = { 0x21, 0x2F, 0x4F, 0x5E, 0xAD, 0x54, 0x39, 0x33, 0x01, 0x91, 0x1E, 0xD2, 0x33, 0x04, 0x00, 0x29, 0x37, 0xA3, 0x6B, 0xA0, 0xC6, 0x3F, 0xAA, 0x7B, 0x66, 0x70, 0x04, 0x0E, 0x91, 0x44, 0x8E, 0x16 };//32 bytes
        static readonly byte[] SettIv = { 0x3B, 0x12, 0x5C, 0x30, 0xAC, 0x4F, 0x80, 0xC8, 0x25, 0xA1, 0x33, 0xC3, 0x13, 0x3E, 0xC6, 0xB4 };//16 bytes

        public static ChatServerDataSetTableAdapters.LoginsTableAdapter LoginsTableAdapterdef;
        public static LogForm LogForm;

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Program));

        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Logger.Debug("Main()");
            Logger.Error("Main()");

            // Visual
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LogForm = new LogForm();            

            AppDomain.CurrentDomain.UnhandledException += (sender, e) => LogException(e.ExceptionObject as Exception);
            Application.ThreadException += (sender, e) => LogException(e.Exception);

            ChatServer.init(Properties.Settings.Default.Port); // must catch invalid pass 
            
            // Init ServerConfig Form            
            new ServerConfig();
            Application.Run();
        }

        #region Connection String

        public static string GetConnectionString()
        {
            string res;
            var aes1 = new AESCSPImpl(SettKey, SettIv);
            try
            {
                res = aes1.DecryptStr(System.Text.HexRep.ToBytes(Properties.Settings.Default.String1)); 
            }
            finally
            {
                aes1.Clear();
            }

            return res;
        }

        public static void SaveConStr(string plaintext)
        {
            var aes1 = new AESCSPImpl(SettKey, SettIv);
            try
            {
                Properties.Settings.Default.String1 = System.Text.HexRep.ToString(aes1.EncryptStr(plaintext));                
            }
            finally
            {
                aes1.Clear();
            }
        }

        #endregion        

        #region Logging & Notify

        public static void LogEvent(string msg)
        {
            Logger.Info(msg);
        }
       
        public static void LogException(Exception ex)
        {            
            Logger.Error(ex);
        }

        #endregion
    }
}
