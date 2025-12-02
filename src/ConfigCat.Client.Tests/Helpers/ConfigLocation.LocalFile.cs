using System;
using System.IO;

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
            return Config.Deserialize(configJson.AsMemory(), tolerant: false);
#else
            return Config.Deserialize(configJson.AsSpan(), tolerant: false);
#endif
        }
    }
}
