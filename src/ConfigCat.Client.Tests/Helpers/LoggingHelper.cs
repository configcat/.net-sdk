using Moq;
using System.Collections.Generic;
using System;

namespace ConfigCat.Client;

internal struct LogEvent
{
    public LogEvent(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception)
    {
        (this.Level, this.EventId, this.Message, this.Exception) = (level, eventId, message, exception);
    }

    public readonly LogLevel Level;
    public readonly LogEventId EventId;
    public FormattableLogMessage Message;
    public readonly Exception? Exception;
}

internal static class LoggingHelper
{
    public static LoggerWrapper AsWrapper(this IConfigCatLogger logger, Hooks? hooks = null)
    {
        return new LoggerWrapper(logger, hooks);
    }

    public static IConfigCatLogger CreateCapturingLogger(List<LogEvent> logEvents, LogLevel logLevel = LogLevel.Info)
    {
        var loggerMock = new Mock<IConfigCatLogger>();

        loggerMock.SetupGet(logger => logger.LogLevel).Returns(logLevel);

        loggerMock.Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()))
            .Callback(delegate (LogLevel level, LogEventId eventId, ref FormattableLogMessage msg, Exception ex) { logEvents.Add(new LogEvent(level, eventId, ref msg, ex)); });

        return loggerMock.Object;
    }
}
