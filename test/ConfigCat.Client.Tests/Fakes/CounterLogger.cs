using System;
using System.Threading;

namespace ConfigCat.Client.Tests;

internal sealed class CounterLogger : IConfigCatLogger
{
    public int LogMessageInvokeCount;

    public LogLevel LogLevel { get; set; }

    public CounterLogger() : this(LogLevel.Debug) { }

    public CounterLogger(LogLevel logLevel)
    {
        LogLevel = logLevel;
    }

    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
    {
        Interlocked.Increment(ref this.LogMessageInvokeCount);
    }
}
