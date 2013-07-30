using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Windows.Forms;
using System.IO;

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
            //Visual
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            logForm = new LogForm();            

            AppDomain.CurrentDomain.UnhandledException += (sender, e) => LogException(e.ExceptionObject as Exception);
            Application.ThreadException += (sender, e) => LogException(e.Exception);

            initLogFile();

            //Database Connection
            if (initTableAdapter())
            {
                ChatServer.init(loginsTableAdapterdef);//must catch invalid pass 
            }
            else
            {
                logForm.LogException(new Exception("Unable to create database connection. See log file for details"));                
                //defMsgBox("Unable to create database connection -> stopping. See log file for details");
                //return;
            }
           
            //Init ServerConfig Form            
            //Application.Run(new ServerConfig());
            ServerConfig serverConfig = new ServerConfig();
            Application.Run();
        }

        #region Inits

        private static void initLogFile()
        {
            try
            {
                logFile = MyChatServer.Properties.Settings.Default.logFile;
            }
            catch (Exception ex)
            {
                logForm.LogException(new Exception(String.Format("Unable to create log file. Using default ({0}).", logFile), ex));
                //defMsgBox(String.Format("Unable to create log file. Reason: {0}{1}{0}Using default.", Environment.NewLine, ex, logFile));
            }
        }

        public static bool initTableAdapter()
        {
            try
            {
                loginsTableAdapterdef = new ChatServerDataSetTableAdapters.LoginsTableAdapter();
                //loginsTableAdapterdef.Connection.ConnectionString = Crypto.Crypto1.decStrDef(Properties.Settings.Default.String1);
                loginsTableAdapterdef.Connection.ConnectionString = loadConStr();
                //MessageBox.Show(aes1.decryptStr(System.Text.HexRep.ToBytes(Properties.Settings.Default.String1)));               

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

        public static string loadConStr()
        {
            string res=null;
            System.Security.Cryptography.AESCSPImpl aes1 = new System.Security.Cryptography.AESCSPImpl(settKey, settIV);
            try
            {
                res = aes1.decryptStr(System.Text.HexRep.ToBytes(Properties.Settings.Default.String1)); 
            }
            finally
            {
                if (aes1 != null)
                    aes1.Clear();
            }
            return res;
        }

        public static void saveConStr(string plaintext)
        {
            System.Security.Cryptography.AESCSPImpl aes1 = new System.Security.Cryptography.AESCSPImpl(settKey, settIV);
            try
            {
                Properties.Settings.Default.String1 = System.Text.HexRep.ToString(aes1.encryptStr(plaintext));                
            }
            finally
            {
                if (aes1!=null)
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

        public static void defMsgBox(string msg)
        {
            MessageBox.Show(msg, "Chat Server");
        }

        private static string getIntBytes(byte[] bts)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(String.Format("{0}", bts.Length));
            foreach (byte bt in bts)
                sb.Append(String.Format("{0}, ", bt));
            return sb.ToString();
        }

        #endregion
    }
}
