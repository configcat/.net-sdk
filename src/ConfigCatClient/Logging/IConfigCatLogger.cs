using System;

namespace ConfigCat.Client;

/// <summary>
/// Defines the interface for the ConfigCat SDK to perform logging.
/// </summary>
/// <remarks>
/// Note for implementers. Until the deprecated <see cref="ILogger"/> interface is removed, this interface needs to extend it for backward compatibility.
/// This means that when implementing this interface, you also need to implement the full <see cref="ILogger"/> interface temporarily.
/// Later, the <see cref="ILogger.LogLevel"/> property will be moved into this interface but the other methods like <see cref="ILogger.Error(string)"/> will be removed.
/// (However, the ConfigCat SDK does not use the old methods internally any more, so you do not need to provide an actual implementation for them.)
/// </remarks>
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
