using System;

namespace ConfigCat.Client;

internal sealed class LoggerWrapper : IConfigCatLogger
{
    private readonly IConfigCatLogger logger;
    private readonly Hooks hooks;

    public LogLevel LogLevel
    {
        get => this.logger.LogLevel;
        set => this.logger.LogLevel = value;
    }

    internal LoggerWrapper(IConfigCatLogger logger, Hooks hooks = null)
    {
        this.logger = logger;
        this.hooks = hooks ?? NullHooks.Instance;
    }

    private bool TargetLogEnabled(LogLevel targetTrace)
    {
        return (byte)targetTrace <= (byte)LogLevel;
    }

    /// <inheritdoc />
    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
    {
        if (TargetLogEnabled(level))
        {
            this.logger.Log(level, eventId, ref message, exception);
        }

        if (level == LogLevel.Error)
        {
            this.hooks.RaiseError(message.InvariantFormattedMessage, exception);
        }
    }
}
