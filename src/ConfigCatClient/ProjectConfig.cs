using System;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client;

internal sealed class ProjectConfig
{
    public static readonly ProjectConfig Empty = new(null, DateTime.MinValue, null);

    public ProjectConfig(SettingsWithPreferences? config, DateTime timeStamp, string? httpETag)
    {
        Config = config;
        TimeStamp = timeStamp;
        HttpETag = httpETag;
    }

    public ProjectConfig With(DateTime timeStamp) => new ProjectConfig(Config, timeStamp, HttpETag);

    public SettingsWithPreferences? Config { get; }
    public DateTime TimeStamp { get; }
    public string? HttpETag { get; }

    [MemberNotNullWhen(false, nameof(Config))]
    public bool IsEmpty => Config is null;

    public bool IsExpired(TimeSpan expiration)
    {
        return IsEmpty || TimeStamp + expiration < DateTime.UtcNow;
    }

    public bool IsNewerThan(ProjectConfig other)
    {
        if (!IsEmpty)
        {
            return other.IsEmpty
                || other.TimeStamp < TimeStamp
                || other.TimeStamp == TimeStamp && HttpETag != other.HttpETag;
        }
        else
        {
            return other.IsEmpty && other.TimeStamp < TimeStamp;
        }
    }

    public static string Serialize(ProjectConfig config)
    {
        // TODO: use standardized format (beware of JSON serializer differences!) + preserve original config JSON?
        return config.Serialize();
    }

    public static ProjectConfig Deserialize(string value)
    {
        return value.Deserialize<ProjectConfig>()
            ?? throw new InvalidOperationException("Invalid config JSON content: " + value);
    }
}
