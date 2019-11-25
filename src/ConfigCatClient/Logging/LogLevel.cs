namespace ConfigCat.Client
{
    /// <summary>
    /// Specifies message's filtering to output for the <see cref="ILogger"/> class.
    /// Debug > Info > Warning > Error > Off
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// No tracing and any debugging messages.
        /// </summary>
        Off = 0,
        /// <summary>
        /// Error messages.
        /// </summary>
        Error = 1,
        /// <summary>
        /// Error and warning messages.
        /// </summary>
        Warning = 2,
        /// <summary>
        /// Information, Error and Warning messages.
        /// </summary>
        Info = 3,
        /// <summary>
        /// All messages
        /// </summary>
        Debug = 4
    }
}
