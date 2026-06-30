using System;
using System.Collections.Concurrent;
using ConfigCat.Client;

namespace ConfigCat.Extensions.Hosting.Tests.Fakes;

internal sealed class FakeConfigCatLogger : IConfigCatLogger
{
    public ConcurrentQueue<(LogLevel logLevel, LogEventId eventId, string message, Exception? exception)> LogEvents { get; } = new();

    public LogLevel LogLevel { get; set; }

    public FakeConfigCatLogger(LogLevel logLevel = LogLevel.Debug)
    {
        LogLevel = logLevel;
    }

    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
    {
        LogEvents.Enqueue((level, eventId, message.ToString(), exception));
    }
}
