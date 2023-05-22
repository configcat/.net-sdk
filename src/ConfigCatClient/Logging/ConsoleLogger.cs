using System;
using System.Globalization;

namespace ConfigCat.Client;

/// <summary>
/// Write log messages into <see cref="Console"/>
/// </summary>
public class ConsoleLogger : IConfigCatLogger
{
    private volatile LogLevel logLevel;

    /// <inheritdoc />
    public LogLevel LogLevel
    {
        get => this.logLevel;
        set => this.logLevel = value;
    }

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

    /// <inheritdoc />
    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
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
