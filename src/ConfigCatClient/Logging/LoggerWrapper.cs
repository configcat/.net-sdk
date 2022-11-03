﻿using System;

namespace ConfigCat.Client
{
    internal sealed class LoggerWrapper : ILogger
    {
        private readonly ILogger logger;
        private readonly Hooks hooks;

        public LogLevel LogLevel
        {
            get => logger.LogLevel;
            set => logger.LogLevel = value;
        }

        internal LoggerWrapper(ILogger logger, Hooks hooks = null)
        {
            this.logger = logger;
            this.hooks = hooks ?? NullHooks.Instance;
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

        public void Error(string message) => Error(message, exception: null);

        public void Error(string message, Exception exception)
        {
            if (this.TargetLogEnabled(LogLevel.Error))
            {
                var logMessage = exception is not null
                    ? message + Environment.NewLine + exception
                    : message;

                this.logger.Error(logMessage);
            }

            this.hooks.RaiseError(message, exception);
        }
    }
}
