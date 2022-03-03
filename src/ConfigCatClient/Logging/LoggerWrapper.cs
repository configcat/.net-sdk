using System;

namespace ConfigCat.Client
{
    internal sealed class LoggerWrapper : ILogger
    {
        public static readonly LoggerWrapper Default = new(new ConsoleLogger());

        private readonly ILogger logger;

        public LogLevel LogLevel { get; set; }

        internal LoggerWrapper(ILogger logger)
        {
            this.LogLevel = logger.LogLevel;
            this.logger = logger;
        }

        private bool TargetLogEnabled(LogLevel targetTrace)
        {
            return (byte)targetTrace <= (byte)this.LogLevel;
        }

        /// <inheritdoc />
        public void Debug(string message)
        {
            if (this.TargetLogEnabled(LogLevel.Debug))
            {
                this.logger.Debug(message);
            }
        }

        /// <inheritdoc />
        public void Information(string message)
        {
            if (this.TargetLogEnabled(LogLevel.Info))
            {
                this.logger.Information(message);
            }
        }

        /// <inheritdoc />
        public void Warning(string message)
        {
            if (this.TargetLogEnabled(LogLevel.Warning))
            {
                this.logger.Warning(message);
            }
        }

        /// <inheritdoc />
        public void Error(string message)
        {
            if (this.TargetLogEnabled(LogLevel.Error))
            {
                this.logger.Error(message);
            }
        }
    }
}
