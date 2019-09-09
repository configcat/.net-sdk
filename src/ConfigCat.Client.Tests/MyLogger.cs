namespace ConfigCat.Client.Tests
{
    internal sealed class MyLogger : LoggerBase
    {
        public byte LogMessageInvokeCount = 0;

        public MyLogger(string loggerName, LogLevel logLevel) : base(loggerName, logLevel)
        {
        }

        protected override void LogMessage(string message)
        {
            LogMessageInvokeCount++;
        }
    }
}


