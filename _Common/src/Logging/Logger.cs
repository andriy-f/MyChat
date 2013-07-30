using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace my.logger
{
    public class MyLogger //:IDisposable
    {
        private StreamWriter _sw = null;
        private string _logFile;


        private int MaxLogSize = 10485760;
        private int logQueueMaxSize = 1;
        //private int logFileMaxBackups = 2;

        private Queue<LoggingItem> logQueue = new Queue<LoggingItem>();

        public string LogFile
        {
            get { return _logFile; }
            set
            {
                try
                {
                    if (_sw != null)
                    {
                        logWhatIsLeft();
                        _sw.Close();
                        _sw = null;
                    }
                    //Attempt to create or open file for writing
                    _sw = new StreamWriter(value, true);

                    //Delete file if too long
                    FileInfo fi = new FileInfo(value);
                    if (fi.Length >= MaxLogSize)
                    {
                        _sw.Close();
                        fi.Delete();
                        _sw = new StreamWriter(value, true);
                    }
                    _logFile = value;
                }
                catch (Exception ex)
                {
                    _sw = null;
                    _logFile = null;
                    Console.Error.WriteLine(
                        string.Format("This exception happened while initializing Logger:\n\n" + ex.Message));
                    //System.Diagnostics.Trace.WriteLine("This exception happened while initializing Logger:\n\n" + ex);
                    //MsgBox(string.Format("This exception happened while initializing Logger:\n\n" + ex.Message));
                }
            }
        }



        public delegate void ErrorLoggingEventHandler(Exception ex);

        public delegate void EventLoggingEventHandler(string evnt);

        public event ErrorLoggingEventHandler ErrorLogged;
        public event EventLoggingEventHandler EventLogged;

        //private void OnEventLogged(string evnt)
        //{
        //    if(EventLogged!=null)
        //        EventLogged
        //}

        public MyLogger(string logfile)
        {
            LogFile = logfile;
        }

        private void logItem(LoggingItem li)
        {
            if (logQueueMaxSize > 1)
            {
                lock (logQueue)
                {
                    logQueue.Enqueue(li);
                    if (logQueue.Count > logQueueMaxSize)
                    {
                        lock (_sw)
                        {
                            foreach (var logItem in logQueue)
                            {
                                _sw.Write(logItem.ToString());
                            }
                            _sw.Flush();
                        }
                        logQueue.Clear();
                    }
                }
            }
            else
                lock (_sw)
                {
                    _sw.Write(li.ToString());
                    _sw.Flush();
                }
        }

        public void logWhatIsLeft()
        {
            if (logQueueMaxSize <= 1) return;
            lock (logQueue)
            {
                lock (_sw)
                {
                    foreach (var logItem in logQueue)
                    {
                        _sw.Write(logItem.ToString());
                    }
                    _sw.Flush();
                }
                logQueue.Clear();
            }
        }


        ~MyLogger()
        {
            //Dirty Trick
            if (logQueue.Count == 0)
                return;
            _sw = null;
            LogFile = _logFile;
            logWhatIsLeft();
            _sw.Dispose();
        }

        public void Event(string _event)
        {
            try
            {
                logItem(new LoggingItem {Message = _event});

            }
            catch (Exception iex)
            {
                Console.WriteLine(GetFmtdEventString(_event + " (This event was not logged"));
                Console.Error.WriteLine("Error while logging event: " + iex);
                //MsgBox(GetFmtdErrorString(iex, "This exception happened while logging event"));
            }

            //Display event via EventLogged -------------------------------
            try
            {
                if (EventLogged != null)
                    EventLogged(_event);
            }
            catch (Exception iex)
            {
                //MsgBox(GetFmtdEventString(_event + " (EventLogged event error)"));
                //MsgBox(GetFmtdErrorString(iex, "EventLogged event error"));
                Console.WriteLine(GetFmtdEventString(_event + " (This event wasn't processed via EventLogged event"));
                Console.Error.WriteLine("Error in EventLogged event: " + iex);
            }
        }

        public void Error(Exception ex)
        {
            Error(ex, null);
        }

        public void Error(Exception ex, object comment)
        {
            try
            {
                logItem(new LoggingItem() {Exception = ex, Message = comment});
            }
            catch (Exception lex)
            {
                //MsgBox(GetFmtdErrorString(ex, "This exception with comment '" + comment + "' was not logged"));
                //MsgBox(GetFmtdErrorString(lex, "This exception happened while logging error"));
                Console.WriteLine(GetFmtdErrorString(ex, "This exception with comment '" + comment + "' was not logged"));
                Console.Error.WriteLine("Exception while logging error: " + lex);
            }

            //ErrorLogged event
            try
            {
                if (ErrorLogged != null)
                    ErrorLogged(ex);
            }
            catch (Exception iex)
            {
                //MsgBox(GetFmtdErrorString(ex, "This exception was not displayed"));
                //MsgBox(GetFmtdErrorString(iex, "This exception happened while displaying exception"));
                Console.WriteLine(GetFmtdErrorString(ex, "This exception with comment '" + comment + "' was not processed via ErrorLogged event"));
                Console.Error.WriteLine("Error in ErrorLogged event: " + iex);
            }

        }

        public static string GetFmtdErrorString(Exception ex, object comment)
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0}----------------------Exception-Start------------------{0}" +
                                 "Time: {1}{0}" +
                                 "Comment: {2}{0}{3}{0}" +
                                 "----------------------Exception-End--------------------{0}",
                                 Environment.NewLine, DateTime.Now, comment, ex);
        }

        public static string GetFmtdEventString(object _event)
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0}{1} {2}{0}",
                                 Environment.NewLine, DateTime.Now, _event);
        }

        #region Disposing stuff

        //private bool disposed = false;

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //protected virtual void Dispose(bool disposing)
        //{
        //    // Check to see if Dispose has already been called.
        //    if (!disposed)
        //    {
        //        if (disposing)
        //        {
        //            //Dispose managed resources
        //            //Managed resources get disposed before destructor that's why destructor calls Dispose(false)
        //            _sw.Close();
        //            //Or _sw.Dispose();//??
        //        }
        //        disposed = true;

        //    }
        //}

        #endregion

        #region LoggingItem

        public class LoggingItem
        {
            //public enum LoggingItemType
            //{
            //    Event,
            //    Exception
            //};

            //private LoggingItemType _loggingItemType;

            public object Message { get; set; }//Must be
            public Exception Exception { get; set; }

            public override string ToString()
            {

                if (Exception != null)
                    return GetFmtdErrorString(Exception, Message);
                else
                    return GetFmtdEventString(Message);
            }
        }

        #endregion
    }
}