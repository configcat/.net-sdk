namespace ConfigCat.Client;

/// <summary>
/// Specifies event severity levels for the <see cref="IConfigCatLogger"/> interface.
/// The levels are interpreted as minimum levels in the case of event filtering.
/// </summary>
/// <remarks>
/// Debug > Info > Warning > Error > Off
/// </remarks>
public enum LogLevel
{
    /// <summary>
    /// No events are logged.
    /// </summary>
    Off = 0,
    /// <summary>
    /// Error events are logged. All other events are discarded.
    /// </summary>
    Error = 1,
    /// <summary>
    /// Warning and Error events are logged. Information and Debug events are discarded.
    /// </summary>
    Warning = 2,
    /// <summary>
    /// Information, Warning and Error are logged. Debug events are discarded.
    /// </summary>
    Info = 3,
    /// <summary>
    /// All events are logged.
    /// </summary>
    Debug = 4
}
