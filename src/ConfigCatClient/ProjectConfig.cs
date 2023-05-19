using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

internal sealed class ProjectConfig
{
    internal const string SerializationFormatVersion = "v1";

    public static readonly ProjectConfig Empty = new(null, null, DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), null);

    public ProjectConfig(string? configJson, SettingsWithPreferences? config, DateTime timeStamp, string? httpETag)
    {
        Debug.Assert(!(configJson is null ^ config is null), $"{nameof(configJson)} and {nameof(config)} must be both null or both not null.");

        ConfigJson = configJson;
        Config = config;
        TimeStamp = timeStamp;
        HttpETag = httpETag;
    }

    public ProjectConfig With(DateTime timeStamp) => new ProjectConfig(ConfigJson, Config, timeStamp, HttpETag);

    public string? ConfigJson { get; }
    public SettingsWithPreferences? Config { get; }
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
        // Remove the sub-second part as we need second precision only.
        return utcNow.AddTicks(-(utcNow.Ticks % TimeSpan.TicksPerSecond));
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
        var configJsonSpan = value.AsSpan(index);

        SettingsWithPreferences? config;
        string? configJson;
        if (configJsonSpan.Length > 0)
        {
            config = configJsonSpan.DeserializeOrDefault<SettingsWithPreferences>();
            if (config is null)
            {
                throw new FormatException("Invalid config JSON content: " + configJsonSpan.ToString());
            }
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
