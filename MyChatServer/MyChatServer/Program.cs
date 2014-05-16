namespace Andriy.MyChat.Server
{
    using System;
    using System.Windows.Forms;

    using Andriy.MyChat.Server.DAL;

    public static class Program
    {
        #region Fields

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Program));

        public static LogForm LogForm { get; private set; }

        internal static ChatServer Server { get; private set; }

        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Logger.Debug("Main()");
            
            // Visual
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LogForm = new LogForm();            

            AppDomain.CurrentDomain.UnhandledException += (sender, e) => LogException(e.ExceptionObject as Exception);
            Application.ThreadException += (sender, e) => LogException(e.Exception);

            Server = new ChatServer(DataContext.Instance, Properties.Settings.Default.Port);
            
            // Init ServerConfig Form            
            new ServerConfig();
            Application.Run();
        }

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
