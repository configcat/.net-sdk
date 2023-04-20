using System;

namespace ConfigCat.Client;

/// <summary>
/// Defines the interface for the ConfigCat SDK to perform logging.
/// </summary>
public interface IConfigCatLogger
{
    /// <summary>
    /// Specifies message filtering.
    /// </summary>
    LogLevel LogLevel { get; set; }

    /// <summary>
    /// Writes a message into the log.
    /// </summary>
    /// <param name="level">Level (event severity).</param>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="message">Message.</param>
    /// <param name="exception">The <see cref="Exception"/> object related to the message (if any).</param>
    void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null);
}
