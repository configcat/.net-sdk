using System;

namespace ConfigCat.Client;

internal sealed class LoggerWrapper : IConfigCatLogger
{
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly ILogger logger; // Backward compatibility, it'll be changed to IConfigCatCache later.
#pragma warning restore CS0618 // Type or member is obsolete
    private readonly Hooks hooks;

    public LogLevel LogLevel
    {
        get => this.logger.LogLevel;
        set => this.logger.LogLevel = value;
    }

#pragma warning disable CS0618 // Type or member is obsolete
    internal LoggerWrapper(ILogger logger, Hooks hooks = null)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        this.logger = logger;
        this.hooks = hooks ?? NullHooks.Instance;
    }

    private bool TargetLogEnabled(LogLevel targetTrace)
    {
        return (byte)targetTrace <= (byte)LogLevel;
    }

    #region Deprecated methods

    void ILogger.Debug(string message) => throw new NotSupportedException();

    void ILogger.Information(string message) => throw new NotSupportedException();

    void ILogger.Warning(string message) => throw new NotSupportedException();

    void ILogger.Error(string message) => throw new NotSupportedException();

    #endregion

    /// <inheritdoc />
    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
    {
        if (TargetLogEnabled(level))
        {
            if (this.logger is IConfigCatLogger configCatLogger)
            {
                configCatLogger.Log(level, eventId, ref message, exception);
            }
            else
            {
                switch (level)
                {
                    case LogLevel.Error:
                        this.logger.Error(message.InvariantFormattedMessage);
                        break;
                    case LogLevel.Warning:
                        this.logger.Warning(message.InvariantFormattedMessage);
                        break;
                    case LogLevel.Info:
                        this.logger.Information(message.InvariantFormattedMessage);
                        break;
                    case LogLevel.Debug:
                        this.logger.Debug(message.InvariantFormattedMessage);
                        break;
                }
            }
        }

        if (level == LogLevel.Error)
        {
            this.hooks.RaiseError(message.InvariantFormattedMessage, exception);
        }
    }
}
