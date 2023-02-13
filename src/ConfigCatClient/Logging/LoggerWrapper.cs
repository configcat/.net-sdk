using System;

namespace ConfigCat.Client;

internal sealed class LoggerWrapper : ILogger
{
    private readonly ILogger logger;
    private readonly Hooks hooks;

    public LogLevel LogLevel
    {
        get => this.logger.LogLevel;
        set => this.logger.LogLevel = value;
    }

    internal LoggerWrapper(ILogger logger, Hooks hooks = null)
    {
        this.logger = logger;
        this.hooks = hooks ?? NullHooks.Instance;
    }

    private bool TargetLogEnabled(LogLevel targetTrace)
    {
        return (byte)targetTrace <= (byte)LogLevel;
    }

    #region Deprecated methods

    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Debug(string message) => throw new NotSupportedException();

    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Information(string message) => throw new NotSupportedException();

    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Warning(string message) => throw new NotSupportedException();

    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Error(string message) => throw new NotSupportedException();

    [Obsolete("This method is obsolete and will be removed from the public API in a future major version. Please use the Log() method instead.")]
    public void Error(string message, Exception exception) => throw new NotSupportedException();

    #endregion

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
