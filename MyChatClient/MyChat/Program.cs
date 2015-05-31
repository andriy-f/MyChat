namespace Andriy.MyChat.Client
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows.Forms;

    public static class Program
    {
        internal static string logFile = @"%TEMP%\MyChat.log";

        private static ChatClient chatClient;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => LogException(e.ExceptionObject as Exception);
            Application.ThreadException += (sender, e) => LogException(e.Exception);

            logFile = Properties.Settings.Default.logFile;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            chatClient = new ChatClient();
            Application.Run(new LogonForm(chatClient));
        }

        #region Logging

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
                    MessageBox.Show(String.Format("{1} Cannot log Event '{2}'{0} because {3}", Environment.NewLine, DateTime.Now, msg, lex));
                }
            }
        }
        
        public static void LogException(Exception ex)
        {
            if (null != ex)
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
                        sw1.WriteLine(string.Format(CultureInfo.InvariantCulture, "{1} ERROR{0}{2}", Environment.NewLine, DateTime.Now, ex));
                        sw1.WriteLine();
                    }
                }
                catch (Exception lex)
                {
                    MessageBox.Show(String.Format("{1} Cannot log Exception '{2}'{0} because {3}", Environment.NewLine, DateTime.Now, ex, lex));
#if DEBUG
                    throw;
#endif
                }
            }
        }

        #endregion
    }
}
