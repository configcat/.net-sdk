using System;
using System.Collections.Concurrent;
using System.IO;

namespace ConfigCat.Client.Tests.Helpers;

internal static class ConfigHelper
{
    public static ProjectConfig FromString(string configJson, string? httpETag, DateTime timeStamp)
    {
        return new ProjectConfig(configJson, Config.Deserialize(configJson.AsSpan()), timeStamp, httpETag);
    }

    public static ProjectConfig FromFile(string configJsonFilePath, string? httpETag, DateTime timeStamp)
    {
        return FromString(File.ReadAllText(configJsonFilePath), httpETag, timeStamp);
    }

    private static readonly ConcurrentDictionary<ConfigLocation, Lazy<Config>> ConfigCache = new();

    public static Config FetchConfigCached(this ConfigLocation location)
    {
        // NOTE: ConfigLocation is a record type, that is, has value equality,
        // which is exactly what we want here w.r.t. the cache key.
        return ConfigCache
            .GetOrAdd(location, _ => new Lazy<Config>(() => location.FetchConfig(), isThreadSafe: true))
            .Value;
    }
}
