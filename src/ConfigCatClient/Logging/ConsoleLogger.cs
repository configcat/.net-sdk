using System;

namespace ConfigCat.Client;

/// <summary>
/// Write log messages into <see cref="Console"/>
/// </summary>
public class ConsoleLogger : ILogger
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
    public void Debug(string message) => this.Log(LogLevel.Debug, eventId: default, message);

    /// <inheritdoc />
    public void Information(string message) => this.Log(LogLevel.Info, eventId: default, message);

    /// <inheritdoc />
    public void Warning(string message) => this.Log(LogLevel.Warning, eventId: default, message);

    /// <inheritdoc />
    public void Error(string message) => this.Log(LogLevel.Error, eventId: default, message);

    #endregion

    /// <inheritdoc />
    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
    {
        var levelString = level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARNING",
            LogLevel.Error => "ERROR",
            _ => level.ToString().ToUpper()
        };

        var exceptionString = exception is not null ? Environment.NewLine + exception : string.Empty;

        // NOTE: levelString.PadRight(7) is intentionally not simplifed to {levelString,-7} because in that case
        // the string interpolation would be translated to a string.Format call instead of the more performant string.Concat call.
        Console.WriteLine($"ConfigCat.{levelString.PadRight(7)} {message.InvariantFormattedMessage}{exceptionString}");
    }
}
