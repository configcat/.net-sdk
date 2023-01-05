using System;

namespace ConfigCat.Client.Tests;

internal sealed class CounterLogger : ILogger
{
    public byte LogMessageInvokeCount = 0;

    public LogLevel LogLevel { get; set; }

    public CounterLogger() : this(LogLevel.Debug) { }

    public CounterLogger(LogLevel logLevel)
    {
        LogLevel = logLevel;
    }

    #region Deprecated methods

    public void Debug(string message) => this.Log(LogLevel.Debug, eventId: default, message);

    public void Information(string message) => this.Log(LogLevel.Info, eventId: default, message);

    public void Warning(string message) => this.Log(LogLevel.Warning, eventId: default, message);

    public void Error(string message) => this.Log(LogLevel.Error, eventId: default, message);

    #endregion

    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
    {
        this.LogMessageInvokeCount++;
    }
}
