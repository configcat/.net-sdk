using System;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client;

internal sealed class LoggerWrapper : IConfigCatLogger
{
    private readonly IConfigCatLogger logger;
    private readonly LogFilterCallback? filter;
    private readonly SafeHooksWrapper hooks;

    public LogLevel LogLevel
    {
        get => this.logger.LogLevel;
        set => this.logger.LogLevel = value;
    }

    internal LoggerWrapper(IConfigCatLogger logger, LogFilterCallback? filter = null, SafeHooksWrapper hooks = default)
    {
        this.logger = logger;
        this.filter = filter;
        this.hooks = hooks;
    }

    public bool IsEnabled(LogLevel level)
    {
        return (byte)level <= (byte)LogLevel;
    }

    /// <inheritdoc />
    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
    {
        if (IsEnabled(level) && (this.filter is null || this.filter(level, eventId, ref message, exception)))
        {
            this.logger.Log(level, eventId, ref message, exception);
        }

        if (level == LogLevel.Error)
        {
            this.hooks.RaiseError(ref message, exception);
        }
    }
}
