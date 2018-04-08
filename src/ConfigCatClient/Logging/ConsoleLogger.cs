using System;

namespace ConfigCat.Client.Logging
{
    /// <summary>
    /// Write log messages into <see cref="Console"/>
    /// </summary>
    public class ConsoleLogger : LoggerBase
    {
        /// <inheritdoc />
        public ConsoleLogger(string loggerName) : base(loggerName, LogLevel.Debug)
        {
        }

        /// <inheritdoc />
        protected override void LogMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
