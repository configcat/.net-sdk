using System;

namespace ConfigCat.Client;

internal sealed class LoggerWrapper : IConfigCatLogger
{
    private readonly IConfigCatLogger logger;
    private readonly SafeHooksWrapper hooks;

    public LogLevel LogLevel
    {
        get => this.logger.LogLevel;
        set => this.logger.LogLevel = value;
    }

    internal LoggerWrapper(IConfigCatLogger logger, SafeHooksWrapper hooks = default)
    {
        this.logger = logger;
        this.hooks = hooks;
    }

    public bool IsEnabled(LogLevel level)
    {
        return (byte)level <= (byte)LogLevel;
    }

    /// <inheritdoc />
    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
    {
        if (IsEnabled(level))
        {
            this.logger.Log(level, eventId, ref message, exception);
        }

        if (level == LogLevel.Error)
        {
            this.hooks.RaiseError(message.InvariantFormattedMessage, exception);
        }
    }
}
