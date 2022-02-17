namespace ConfigCat.Client.Tests
{
    internal sealed class CounterLogger : ILogger
    {
        public byte LogMessageInvokeCount = 0;

        public LogLevel LogLevel { get; set; }

        public CounterLogger() : this(LogLevel.Debug) { }

        public CounterLogger(LogLevel logLevel)
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


