using System;

namespace ConfigCat.Client;

internal static partial class LoggerExtensions
{
    public static FormattableLogMessage Log(this LoggerWrapper logger, LogLevel level, LogEventId eventId, string message)
    {
        return Log(logger, level, eventId, exception: null, message);
    }

    public static FormattableLogMessage Log(this LoggerWrapper logger, LogLevel level, LogEventId eventId, Exception? exception, string message)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var logMessage = new FormattableLogMessage(message);
        logger.Log(level, eventId, ref logMessage, exception);
        return logMessage;
    }

    public static FormattableLogMessage LogFormatted(this LoggerWrapper logger, LogLevel level, LogEventId eventId, string messageFormat, string[] argNames, object[] argValues)
    {
        return LogFormatted(logger, level, eventId, exception: null, messageFormat, argNames, argValues);
    }

    public static FormattableLogMessage LogFormatted(this LoggerWrapper logger, LogLevel level, LogEventId eventId, Exception? exception, string messageFormat, string[] argNames, object[] argValues)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var logMessage = new FormattableLogMessage(messageFormat, argNames, argValues);
        logger.Log(level, eventId, ref logMessage, exception);
        return logMessage;
    }

    // The LogInterpolated methods make more convenient to log messages by allowing us to write
    // `logger.LogInterpolated(LogLevel.Error, 1234, $"A message with {paramValue}", "PARAM")`
    // instead of
    // `logger.LogFormatted(LogLevel.Error, 1234, "A message with {0}", new[] { "PARAM" }, new[] { paramValue })`.
    // (Alternatively, we could take the approach of Microsoft.Extensions.Logging, which would look like
    // `logger.Log(LogLevel.Error, 1234, "A message with {PARAM}", paramValue)`
    // but then we'd need to manually parse the format string, which is better to avoid to keep our solution simple.)

    public static FormattableLogMessage LogInterpolated(this LoggerWrapper logger, LogLevel level, LogEventId eventId, ValueFormattableString message, params string[] argNames)
    {
        return LogInterpolated(logger, level, eventId, exception: null, message, argNames);
    }

    public static FormattableLogMessage LogInterpolated(this LoggerWrapper logger, LogLevel level, LogEventId eventId, Exception? exception, ValueFormattableString message, params string[] argNames)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var logMessage = FormattableLogMessage.FromInterpolated(message, argNames);
        logger.Log(level, eventId, ref logMessage, exception);
        return logMessage;
    }

    /// <summary>
    /// Shorthand method for
    /// <code>logger.Log(LogLevel.Debug, default, message);</code>
    /// </summary>
    public static FormattableLogMessage Debug(this LoggerWrapper logger, string message) => logger.Log(LogLevel.Debug, default, message);
}
