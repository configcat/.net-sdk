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

    #region Deprecated methods

    void ILogger.Debug(string message) => throw new NotSupportedException();

    void ILogger.Information(string message) => throw new NotSupportedException();

    void ILogger.Warning(string message) => throw new NotSupportedException();

    void ILogger.Error(string message) => throw new NotSupportedException();

    #endregion

    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
    {
        this.LogMessageInvokeCount++;
    }
}
