using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Write log messages into <see cref="Console"/>
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <inheritdoc />
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Create a <see cref="ConsoleLogger"/> instance with Warning loglevel
        /// </summary>
        public ConsoleLogger() : this(LogLevel.Warning) { }

        /// <summary>
        /// Create a <see cref="ConsoleLogger"/> instance
        /// </summary>
        /// <param name="logLevel">Log level</param>
        public ConsoleLogger(LogLevel logLevel)
        {
            this.LogLevel = logLevel;
        }

        /// <inheritdoc />
        public void Debug(string message)
        {
            Console.WriteLine(FormatMessage(LogLevel.Debug, message));
        }

        /// <inheritdoc />
        public void Error(string message)
        {
            Console.WriteLine(FormatMessage(LogLevel.Error, message));
        }

        /// <inheritdoc />
        public void Information(string message)
        {
            Console.WriteLine(FormatMessage(LogLevel.Info, message));
        }

        /// <inheritdoc />
        public void Warning(string message)
        {
            Console.WriteLine(FormatMessage(LogLevel.Warning, message));
        }

        private string FormatMessage(LogLevel logLevel, string message)
        {
            return $"ConfigCat - {logLevel} - {message}";
        }
    }
}
