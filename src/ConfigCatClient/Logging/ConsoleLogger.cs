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
            PrintMessage(LogLevel.Debug, message);
        }

        /// <inheritdoc />
        public void Error(string message)
        {
            PrintMessage(LogLevel.Error, message);
        }

        /// <inheritdoc />
        public void Information(string message)
        {
            PrintMessage(LogLevel.Info, message);
        }

        /// <inheritdoc />
        public void Warning(string message)
        {
            PrintMessage(LogLevel.Warning, message);
        }

        private void PrintMessage(LogLevel logLevel, string message)
        {
            string logLevelPadded = logLevel.ToString().ToUpper().PadRight(7);
            Console.WriteLine($"ConfigCat.{logLevelPadded} {message}");
        }
    }
}
