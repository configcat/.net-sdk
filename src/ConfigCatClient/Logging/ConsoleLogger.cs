using System;
using System.Globalization;

namespace ConfigCat.Client;

/// <summary>
/// Write log messages into <see cref="Console"/>
/// </summary>
public class ConsoleLogger : IConfigCatLogger
{
    /// <inheritdoc />
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Create a <see cref="ConsoleLogger"/> instance with Warning loglevel
    /// </summary>
    public ConsoleLogger() : this(LogLevel.Warning) { }

    /// <summary>
    /// Create a <see cref="ConsoleLogger"/> instance
    /// </summary>
    /// <param name="logLevel">Log level</param>
    public ConsoleLogger(LogLevel logLevel)
    {
        LogLevel = logLevel;
    }

    #region Deprecated methods

    /// <inheritdoc />
    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Debug(string message)
    {
        var logMessage = new FormattableLogMessage(message);
        Log(LogLevel.Debug, eventId: default, ref logMessage);
    }

    /// <inheritdoc />
    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Information(string message)
    {
        var logMessage = new FormattableLogMessage(message);
        Log(LogLevel.Info, eventId: default, ref logMessage);
    }

    /// <inheritdoc />
    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Warning(string message)
    {
        var logMessage = new FormattableLogMessage(message);
        Log(LogLevel.Warning, eventId: default, ref logMessage);
    }

    /// <inheritdoc />
    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Error(string message)
    {
        var logMessage = new FormattableLogMessage(message);
        Log(LogLevel.Error, eventId: default, ref logMessage);
    }

    #endregion

    /// <inheritdoc />
    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
    {
        var levelString = level switch
        {
#pragma warning disable format
            LogLevel.Debug   => "DEBUG",
            LogLevel.Info    => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error   => "ERROR",
#pragma warning restore format
            _ => level.ToString().ToUpperInvariant().PadRight(5)
        };

        var eventIdString = eventId.Id.ToString(CultureInfo.InvariantCulture);

        var exceptionString = exception is null ? string.Empty : Environment.NewLine + exception;

        Console.WriteLine($"ConfigCat.{levelString} [{eventIdString}] {message.InvariantFormattedMessage}{exceptionString}");
    }
}
