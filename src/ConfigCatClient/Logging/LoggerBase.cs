using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Provides the abstract base class for the Logger
    /// </summary>
    public abstract class LoggerBase : ILogger
    {
        private readonly LogLevel logLevel;

        private readonly string loggerName;

        /// <summary>
        /// Initializes a new instance of the TraceWriterBase class.
        /// </summary>
        /// <param name="loggerName">Name of logger instance</param>
        /// <param name="logLevel">Message filter</param>
        protected LoggerBase(string loggerName, LogLevel logLevel)
        {
            this.loggerName = loggerName;
            this.logLevel = logLevel;
        }

        private bool TargetLogEnabled(LogLevel targetTrace)
        {
            return (byte)targetTrace >= (byte)this.logLevel;
        }

        /// <summary>
        /// Writes a specified <paramref name="message"/> to Logger
        /// </summary>
        /// <param name="message"></param>
        protected abstract void LogMessage(string message);

        /// <summary>
        /// Modify message before write into Logger
        /// </summary>
        /// <param name="logLevel">log level</param>
        /// <param name="message">message</param>
        /// <returns></returns>
        protected virtual string FormatMessage(LogLevel logLevel, string message)
        {
            return $"{DateTime.UtcNow.ToString("yyyy.MM.dd. HH:mm:ss")} - [{logLevel.ToString()}] - {loggerName ?? ""} - {message}";
        }

        /// <summary>
        /// Write a message into a Logger if a passed <paramref name="logLevel"/> is enabled
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        protected void Log(LogLevel logLevel, string message)
        {
            if (this.TargetLogEnabled(logLevel))
            {
                this.LogMessage(FormatMessage(logLevel, message));
            }
        }

        /// <inheritdoc />
        public virtual void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <inheritdoc />
        public virtual void Information(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <inheritdoc />
        public virtual void Warning(string message)
        {
            Log(LogLevel.Warn, message);
        }

        /// <inheritdoc />
        public virtual void Error(string message)
        {
            Log(LogLevel.Error, message);
        }
    }
}
