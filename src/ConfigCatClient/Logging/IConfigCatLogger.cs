using System;

namespace ConfigCat.Client;

/// <summary>
/// Defines the interface for the ConfigCat SDK to perform logging.
/// </summary>
public interface IConfigCatLogger :
#pragma warning disable CS0618 // Type or member is obsolete
    ILogger
#pragma warning restore CS0618 // Type or member is obsolete
{
    /// <summary>
    /// Writes a message into the log.
    /// </summary>
    /// <param name="level">Level (event severity).</param>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="message">Message.</param>
    /// <param name="exception">The <see cref="Exception"/> object related to the message (if any).</param>
    void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null);
}
