using System;
using System.Globalization;

namespace ConfigCat.Client;

/// <summary>
/// An implementation of <see cref="IConfigCatLogger"/> which writes log messages into <see cref="Console"/>.
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
    /// Initializes a new instance of the <see cref="ConsoleLogger"/> class with <see cref="LogLevel.Warning"/>.
    /// </summary>
    public ConsoleLogger() : this(LogLevel.Warning) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogger"/> class with the specified log level.
    /// </summary>
    /// <param name="logLevel">Log level (the minimum level to use for filtering log events).</param>
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
