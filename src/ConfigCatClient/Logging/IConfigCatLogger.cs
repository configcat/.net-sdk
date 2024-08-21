using System;

namespace ConfigCat.Client;

/// <summary>
/// Represents a method that is called by the SDK to decide whether a log event should be logged.
/// </summary>
/// <param name="level">Event severity level.</param>
/// <param name="eventId">Event identifier.</param>
/// <param name="message">Message.</param>
/// <param name="exception">The <see cref="Exception"/> object related to the message (if any).</param>
/// <returns><see langword="true"/> when the event should be logged, <see langword="false"/> when it should be skipped.</returns>
public delegate bool LogFilterCallback(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception);

/// <summary>
/// Defines the interface used by the ConfigCat SDK to perform logging.
/// </summary>
public interface IConfigCatLogger
{
    /// <summary>
    /// Gets or sets the log level (the minimum level to use for filtering log events).
    /// </summary>
    LogLevel LogLevel { get; set; }

    /// <summary>
    /// Writes an event into the log.
    /// </summary>
    /// <param name="level">Event severity level.</param>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="message">Message.</param>
    /// <param name="exception">The <see cref="Exception"/> object related to the message (if any).</param>
    void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null);
}
