using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using ConfigCat.Client;
using Microsoft.Extensions.Logging;

namespace ConfigCat.HostingIntegration.Adapters;

public class ConfigCatToMSLoggerAdapter(ILogger<ConfigCatClient> logger) : IConfigCatLogger
{
    private readonly ILogger logger = logger;
    private readonly ConcurrentDictionary<OriginalFormatCacheKey, string> originalFormatCache = new();

    // Allow all log levels here and let MS logger do log level filtering.
    public Client.LogLevel LogLevel
    {
        get => Client.LogLevel.Debug;
        set { throw new NotSupportedException(); }
    }

    public void Log(Client.LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
    {
        var logLevel = level switch
        {
            Client.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            Client.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            Client.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
            Client.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            _ => Microsoft.Extensions.Logging.LogLevel.None
        };

        var logValues = new LogValues(message, this.originalFormatCache);

        this.logger.Log(logLevel, eventId.Id, state: logValues, exception, LogValues.Formatter);
    }

    // Support for structured logging.
    private struct LogValues(in FormattableLogMessage message, ConcurrentDictionary<OriginalFormatCacheKey, string> originalFormatCache)
        : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public static readonly Func<LogValues, Exception?, string> Formatter = (state, _) => state.ToString();

        private FormattableLogMessage message = message;
        private readonly ConcurrentDictionary<OriginalFormatCacheKey, string> originalFormatCache = originalFormatCache;

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

            return this.originalFormatCache.GetOrAdd(new OriginalFormatCacheKey(this.message), static key =>
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

    private readonly struct OriginalFormatCacheKey(in FormattableLogMessage message) : IEquatable<OriginalFormatCacheKey>
    {
        public readonly string Format = message.Format;
        public readonly string[] ArgNames = message.ArgNames;

        public bool Equals(OriginalFormatCacheKey other) => ReferenceEquals(this.Format, other.Format);

        public override bool Equals(object? obj) => obj is OriginalFormatCacheKey other && Equals(other);

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.Format);
    }
}
