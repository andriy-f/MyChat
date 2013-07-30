using System;
using log4net.Appender;
using log4net.Core;

public class Log4NetLogEventSourceAppender : AppenderSkeleton
{
    private readonly Object _syncRoot;

    public Log4NetLogEventSourceAppender()

    {
        _syncRoot = new object();
    }


    /// <summary>
    /// Occurs when [on log].
    /// </summary>
    public static event EventHandler<OnLog4NetLogEventArgs> OnLog;


    protected override void Append(LoggingEvent loggingEvent)
    {
        EventHandler<OnLog4NetLogEventArgs> temp = OnLog;

        if (temp != null)
        {
            lock (_syncRoot)
            {
                temp(null, new OnLog4NetLogEventArgs(loggingEvent));
            }
        }
    }
}


public class OnLog4NetLogEventArgs : EventArgs
{
    public LoggingEvent LoggingEvent { get; private set; }


    public OnLog4NetLogEventArgs(LoggingEvent loggingEvent)
    {
        LoggingEvent = loggingEvent;
    }
}
