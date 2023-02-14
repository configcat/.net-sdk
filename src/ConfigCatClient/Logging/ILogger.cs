using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides logging interface
/// </summary>
[Obsolete("This interface is obsolete and will be removed from the public API in a future major version. Please use the IConfigCatLogger interface instead.")]
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
    void Debug(string message);

    /// <summary>
    /// Write information level message into log
    /// </summary>
    /// <param name="message"></param>
    void Information(string message);

    /// <summary>
    /// Write warning level message into log
    /// </summary>
    /// <param name="message"></param>
    void Warning(string message);

    /// <summary>
    /// Write error level message into log
    /// </summary>
    /// <param name="message"></param>
    void Error(string message);
}
