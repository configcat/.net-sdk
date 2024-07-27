using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ConfigCat.Client.Extensions.Adapters;

public class ConfigCatToMSLoggerAdapter : ConfigCat.Client.IConfigCatLogger
{
    private readonly ILogger logger;

    public ConfigCatToMSLoggerAdapter(ILogger<ConfigCat.Client.ConfigCatClient> logger)
    {
        this.logger = logger;
    }

    // Allow all log levels here and let MS logger do log level filtering (see appsettings.json)
    public ConfigCat.Client.LogLevel LogLevel
    {
        get => ConfigCat.Client.LogLevel.Debug;
        set { }
    }

    public void Log(ConfigCat.Client.LogLevel level, ConfigCat.Client.LogEventId eventId, ref ConfigCat.Client.FormattableLogMessage message, Exception? exception = null)
    {
        var logLevel = level switch
        {
            ConfigCat.Client.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            ConfigCat.Client.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            ConfigCat.Client.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
            ConfigCat.Client.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            _ => Microsoft.Extensions.Logging.LogLevel.None
        };

        var logValues = new LogValues(ref message);

        this.logger.Log(logLevel, eventId.Id, state: logValues, exception, static (state, _) => state.Message.ToString());

        message = logValues.Message;
    }

    // Support for structured logging.
    private sealed class LogValues : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public LogValues(ref ConfigCat.Client.FormattableLogMessage message)
        {
            Message = message;
        }

        public ConfigCat.Client.FormattableLogMessage Message { get; private set; }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                return index == Count - 1
                    ? new KeyValuePair<string, object?>("{OriginalFormat}", Message.Format)
                    : new KeyValuePair<string, object?>(Message.ArgNames![index], Message.ArgValues![index]);
            }
        }

        public int Count => (Message.ArgNames?.Length ?? 0) + 1;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0, n = Count; i < n; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => Message.InvariantFormattedMessage;
    }
}
