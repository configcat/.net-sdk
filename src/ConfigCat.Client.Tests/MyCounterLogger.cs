namespace ConfigCat.Client.Tests
{
    internal sealed class MyCounterLogger : ILogger
    {
        public byte LogMessageInvokeCount = 0;

        public LogLevel LogLevel { get; set; }

        public MyCounterLogger() : this(LogLevel.Debug) { }

        public MyCounterLogger(LogLevel logLevel)
        {
            this.LogLevel = logLevel;
        }

        public void Debug(string message)
        {
            LogMessage(message);
        }

        public void Error(string message)
        {
            LogMessage(message);
        }

        public void Information(string message)
        {
            LogMessage(message);
        }

        public void Warning(string message)
        {
            LogMessage(message);
        }

        private void LogMessage(string message)
        {
            LogMessageInvokeCount++;
        }
    }
}


