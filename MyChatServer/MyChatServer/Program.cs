using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Windows.Forms;
using System.IO;

using My.Cryptography;

namespace MyChatServer
{
    static class Program
    {
        #region Fields

        static readonly byte[] settKey = { 0x21, 0x2F, 0x4F, 0x5E, 0xAD, 0x54, 0x39, 0x33, 0x01, 0x91, 0x1E, 0xD2, 0x33, 0x04, 0x00, 0x29, 0x37, 0xA3, 0x6B, 0xA0, 0xC6, 0x3F, 0xAA, 0x7B, 0x66, 0x70, 0x04, 0x0E, 0x91, 0x44, 0x8E, 0x16 };//32 bytes
        static readonly byte[] settIV = { 0x3B, 0x12, 0x5C, 0x30, 0xAC, 0x4F, 0x80, 0xC8, 0x25, 0xA1, 0x33, 0xC3, 0x13, 0x3E, 0xC6, 0xB4 };//16 bytes

        static string logFile = @"%TEMP%\MyChatServer.log";
        public static ChatServerDataSetTableAdapters.LoginsTableAdapter loginsTableAdapterdef;
        public static LogForm logForm;

        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Visual
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            logForm = new LogForm();            

            AppDomain.CurrentDomain.UnhandledException += (sender, e) => LogException(e.ExceptionObject as Exception);
            Application.ThreadException += (sender, e) => LogException(e.Exception);

            InitLogFile();

            ChatServer.init(); // must catch invalid pass 
            
            // Init ServerConfig Form            
            new ServerConfig();
            Application.Run();
        }

        #region Inits

        private static void InitLogFile()
        {
            try
            {
                logFile = Properties.Settings.Default.logFile;
            }
            catch (Exception ex)
            {
                logForm.LogException(new Exception(string.Format("Unable to create log file. Using default ({0}).", logFile), ex));
                Console.Error.WriteLine("Log init failed" + ex);
            }
        }

        public static bool InitTableAdapter()
        {
            try
            {
                loginsTableAdapterdef = new ChatServerDataSetTableAdapters.LoginsTableAdapter();
                loginsTableAdapterdef.Connection.ConnectionString = GetConnectionString();
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        #endregion

        #region Connection String

        public static string GetConnectionString()
        {
            string res;
            var aes1 = new AESCSPImpl(settKey, settIV);
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
            var aes1 = new AESCSPImpl(settKey, settIV);
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
            if (null != msg)
            {
                try
                {
                    if (File.Exists(logFile))
                    {
                        FileInfo fi = new FileInfo(logFile);
                        if (fi.Length > 10485760)
                            fi.Delete();
                    }

                    using (StreamWriter sw1 = new StreamWriter(logFile, true))
                    {
                        sw1.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", DateTime.Now, msg));
                        sw1.WriteLine();
                    }
                }
                catch (Exception lex)
                {
                    logForm.LogException(new Exception(String.Format("{1} Cannot log Event '{2}'{0} because {3}", Environment.NewLine, DateTime.Now, msg, lex)));
                    //defMsgBox(String.Format("{1} Cannot log Event '{2}'{0} because {3}", Environment.NewLine, DateTime.Now, msg, lex));
                }
            }
        }
       
        public static void LogException(Exception ex)
        {            
            if (null != ex)
            {
                
                try
                {//throw new Exception("Test");                    
                    if (File.Exists(logFile))
                    {
                        FileInfo fi = new FileInfo(logFile);
                        if (fi.Length > 10485760)
                            fi.Delete();
                    }

                    using (StreamWriter sw1 = new StreamWriter(logFile, true))
                    {
                        sw1.WriteLine(string.Format(CultureInfo.InvariantCulture, "{1} ERROR{0}{2}", Environment.NewLine, DateTime.Now, ex));
                        sw1.WriteLine();
                    }
                }
                catch (Exception lex)
                {
                    logForm.LogException(new Exception(String.Format("{1} Cannot log Exception {2}{0}", Environment.NewLine, DateTime.Now, ex), lex));
                    //defMsgBox(String.Format("{1} Cannot log Exception {2}{0} because {3}", Environment.NewLine, DateTime.Now, ex, lex));
#if DEBUG
                    throw;
#endif
                }
            }
        }

        #endregion
    }
}
