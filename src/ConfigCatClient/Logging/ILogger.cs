using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides logging interface
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Specifies message's filtering
    /// </summary>
    LogLevel LogLevel { get; set; }

    /// <summary>
    /// Write debug level message into log
    /// </summary>
    /// <param name="message"></param>
    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    void Debug(string message);

    /// <summary>
    /// Write information level message into log
    /// </summary>
    /// <param name="message"></param>
    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    void Information(string message);

    /// <summary>
    /// Write warning level message into log
    /// </summary>
    /// <param name="message"></param>
    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    void Warning(string message);

    /// <summary>
    /// Write error level message into log
    /// </summary>
    /// <param name="message"></param>
    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    void Error(string message);

    /// <summary>
    /// Writes a message into the log.
    /// </summary>
    /// <param name="level">Level (event severity).</param>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="message">Message.</param>
    /// <param name="exception">The <see cref="Exception"/> object related to the message (if any).</param>
    void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null);
}
