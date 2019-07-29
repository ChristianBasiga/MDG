using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using log4net.Appender;
using log4net.Core;



namespace MDG.Logging
{
    public class UnityLogAppender : AppenderSkeleton
    {
        delegate void LogMethod(string msg);
        static readonly Dictionary<Level, LogMethod> logMethods = new Dictionary<Level, LogMethod>()
    {
        { Level.Debug, Debug.Log },
        { Level.Error, Debug.LogError },
        { Level.Warn, Debug.LogWarning }
    };

        protected override void Append(LoggingEvent loggingEvent)
        {
            LogMethod logMethod = logMethods[loggingEvent.Level];
            string message = RenderLoggingEvent(loggingEvent);
            logMethod(message);
        }
    }
}