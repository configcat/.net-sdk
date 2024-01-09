using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

internal sealed class ProjectConfig
{
    internal const string SerializationFormatVersion = "v2";

    public static readonly ProjectConfig Empty = new(null, null, DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), null);

    public ProjectConfig(string? configJson, Config? config, DateTime timeStamp, string? httpETag)
    {
        Debug.Assert(!(configJson is null ^ config is null), $"{nameof(configJson)} and {nameof(config)} must be both null or both not null.");
        Debug.Assert(timeStamp.Kind == DateTimeKind.Utc, "Timestamp must be a UTC datetime.");

        ConfigJson = configJson;
        Config = config;
        TimeStamp = timeStamp;
        HttpETag = httpETag;
    }

    public ProjectConfig With(DateTime timeStamp) => new ProjectConfig(ConfigJson, Config, timeStamp, HttpETag);

    public string? ConfigJson { get; }
    public Config? Config { get; }
    public DateTime TimeStamp { get; }
    public string? HttpETag { get; }

    [MemberNotNullWhen(false, nameof(Config))]
    [MemberNotNullWhen(false, nameof(ConfigJson))]
    public bool IsEmpty => Config is null;

    public bool IsExpired(TimeSpan expiration)
    {
        return ReferenceEquals(this, Empty) || TimeStamp + expiration < GenerateTimeStamp();
    }

    public static DateTime GenerateTimeStamp()
    {
        var utcNow = DateTime.UtcNow;
        // Remove the sub-millisecond part as we need millisecond precision only.
        return utcNow.AddTicks(-(utcNow.Ticks % TimeSpan.TicksPerMillisecond));
    }

    public static string Serialize(ProjectConfig config)
    {
        return config.TimeStamp.ToUnixTimeStamp() + "\n"
            + config.HttpETag + "\n"
            + config.ConfigJson;
    }

    public static ProjectConfig Deserialize(string value)
    {
        Span<int> separatorIndices = stackalloc int[2];
        var index = 0;
        for (var i = 0; i < separatorIndices.Length; i++)
        {
            index = value.IndexOf('\n', startIndex: index);
            if (index < 0)
            {
                throw new FormatException("Number of values is fewer than expected.");
            }

            separatorIndices[i] = index;
            index++;
        }

        var endIndex = separatorIndices[0];
        var fetchTimeSpan = value.AsSpan(0, endIndex);

        if (!DateTimeUtils.TryParseUnixTimeStamp(fetchTimeSpan, out var fetchTime))
        {
            throw new FormatException("Invalid fetch time: " + fetchTimeSpan.ToString());
        }

        index = endIndex + 1;
        endIndex = separatorIndices[1];
        var httpETagSpan = value.AsSpan(index, endIndex - index);

        index = endIndex + 1;
        var configJsonSpan = value.AsMemory(index);

        Config? config;
        string? configJson;
        if (configJsonSpan.Length > 0)
        {
            config = Config.Deserialize(configJsonSpan);
            configJson = configJsonSpan.ToString();
        }
        else
        {
            config = null;
            configJson = null;
        }

        return new ProjectConfig(configJson, config, fetchTime, httpETagSpan.Length > 0 ? httpETagSpan.ToString() : null);
    }
}
