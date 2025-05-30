using System;
using System.IO;

#if BENCHMARK_OLD
using Config = ConfigCat.Client.SettingsWithPreferences;
#endif

namespace ConfigCat.Client.Tests.Helpers;

public partial record class ConfigLocation
{
    public sealed record class LocalFile : ConfigLocation
    {
        public LocalFile(params string[] paths) => FilePath = Path.Combine(paths);

        public string FilePath { get; }

        public override string GetRealLocation() => FilePath;

        internal override Config FetchConfig()
        {
            using Stream stream = File.OpenRead(FilePath);
            using StreamReader reader = new(stream);
            var configJson = reader.ReadToEnd();
#if BENCHMARK_OLD
            return configJson.Deserialize<Config>() ?? throw new InvalidOperationException("Invalid config JSON content: " + configJson);
#else
            return Config.Deserialize(configJson.AsSpan());
#endif
        }
    }
}
