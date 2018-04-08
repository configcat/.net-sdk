namespace ConfigCat.Client.Logging
{
    /// <summary>
    /// Provides logger factory interface
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Create a ILogger instance by name
        /// </summary>
        /// <param name="loggerName">Name of logger (expample: ClassName, Local unit name)</param>
        /// <returns></returns>
        ILogger GetLogger(string loggerName);
    }
}
