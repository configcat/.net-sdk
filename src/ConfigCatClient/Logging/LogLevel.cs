namespace ConfigCat.Client;

/// <summary>
/// Specifies message's filtering to output for the <see cref="ILogger"/> class.
/// Debug > Info > Warning > Error > Off
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// No messages are logged.
    /// </summary>
    Off = 0,
    /// <summary>
    /// Error messages are logged. All other messages are discarded.
    /// </summary>
    Error = 1,
    /// <summary>
    /// Warning and Error messages should be logged. Information and Debug messages are discarded.
    /// </summary>
    Warning = 2,
    /// <summary>
    /// Information, Warning and Error are logged. Debug messages are discarded.
    /// </summary>
    Info = 3,
    /// <summary>
    /// All messages should be logged.
    /// </summary>
    Debug = 4
}
