using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ConfigCat.Client.Extensions.Adapters;

public class ConfigCatToMSLoggerAdapter : ConfigCat.Client.IConfigCatLogger
{
    private readonly ILogger logger;
    private readonly ConcurrentDictionary<MessageFormatKey, string> originalFormatCache = new();

    public ConfigCatToMSLoggerAdapter(ILogger<ConfigCat.Client.ConfigCatClient> logger)
    {
        this.logger = logger;
    }

    // Allow all log levels here and let MS logger do log level filtering (see appsettings.json)
    public ConfigCat.Client.LogLevel LogLevel
    {
        get => ConfigCat.Client.LogLevel.Debug;
        set { throw new NotSupportedException(); }
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

        var logValues = new LogValues(message, this.originalFormatCache);

        this.logger.Log(logLevel, eventId.Id, state: logValues, exception, LogValues.Formatter);
    }

    // Support for structured logging.
    private struct LogValues : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public static readonly Func<LogValues, Exception?, string> Formatter = (state, _) => state.ToString();

        private ConfigCat.Client.FormattableLogMessage message;
        private readonly ConcurrentDictionary<MessageFormatKey, string> originalFormatCache;

        public LogValues(in ConfigCat.Client.FormattableLogMessage message, ConcurrentDictionary<MessageFormatKey, string> messageFormatCache)
        {
            this.message = message;
            this.originalFormatCache = messageFormatCache;
        }

        public readonly int Count => (this.message.ArgNames?.Length ?? 0) + 1;

        public readonly KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (this.message.ArgNames is { } argNames && (uint)index < (uint)argNames.Length)
                {
                    return new KeyValuePair<string, object?>(argNames[index], this.message.ArgValues![index]);
                }

                if (index == Count - 1)
                {
                    return new KeyValuePair<string, object?>("{OriginalFormat}", GetOriginalFormat());
                }

                throw new IndexOutOfRangeException(nameof(index));
            }
        }

        private readonly string GetOriginalFormat()
        {
            if (this.message.ArgNames is not { Length: > 0 })
            {
                return this.message.Format;
            }

            return this.originalFormatCache.GetOrAdd(new MessageFormatKey(this.message), key =>
            {
                var argNamePlaceholders = Array.ConvertAll(key.ArgNames, name => "{" + name + "}");
                return string.Format(CultureInfo.InvariantCulture, key.Format, argNamePlaceholders);
            });
        }

        public readonly IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0, n = Count; i < n; i++)
            {
                yield return this[i];
            }
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => this.message.InvariantFormattedMessage;
    }

    private readonly struct MessageFormatKey : IEquatable<MessageFormatKey>
    {
        public readonly string Format;
        public readonly string[] ArgNames;

        public MessageFormatKey(in FormattableLogMessage message)
        {
            this.Format = message.Format;
            this.ArgNames = message.ArgNames;
        }

        public bool Equals(MessageFormatKey other) => ReferenceEquals(this.Format, other.Format);

        public override bool Equals(object? obj) => obj is MessageFormatKey && Equals((MessageFormatKey)obj);

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.Format);
    }
}
