using System.Collections.Generic;

namespace ConfigCat.Client.Tests.Helpers;

internal static class ConfigHelper
{
    public static Dictionary<string, Setting> GetSettings(this Config config) =>
#if BENCHMARK_OLD
        config.Settings;
#else
        config.SettingsOrEmpty;
#endif

    public static Config FetchConfigCached(this ConfigLocation location) => location.FetchConfig();
}
