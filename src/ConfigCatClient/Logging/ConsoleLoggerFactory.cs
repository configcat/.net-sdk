namespace ConfigCat.Client
{
    /// <summary>
    /// Logger factory class for <see cref="ConsoleLogger"/>
    /// </summary>
    public sealed class ConsoleLoggerFactory : ILoggerFactory
    {
        /// <inheritdoc />
        public ILogger GetLogger(string loggerName)
        {
            return new ConsoleLogger(loggerName);
        }
    }
}
