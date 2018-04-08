namespace ConfigCat.Client.Logging
{    
    internal class NullLoggerFactory : ILoggerFactory
    {
        public ILogger GetLogger(string loggerName)
        {
            return new NullLogger();
        }
    }

    internal class NullLogger : LoggerBase
    {
        public NullLogger() : base(null, LogLevel.Off) { }

        protected override void LogMessage(string message) { }
    }
}
