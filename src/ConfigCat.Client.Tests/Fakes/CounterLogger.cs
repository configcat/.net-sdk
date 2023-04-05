using System;

namespace ConfigCat.Client.Tests;

internal sealed class CounterLogger : IConfigCatLogger
{
    public byte LogMessageInvokeCount = 0;

    public LogLevel LogLevel { get; set; }

    public CounterLogger() : this(LogLevel.Debug) { }

    public CounterLogger(LogLevel logLevel)
    {
        LogLevel = logLevel;
    }

    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
    {
        this.LogMessageInvokeCount++;
    }
}
