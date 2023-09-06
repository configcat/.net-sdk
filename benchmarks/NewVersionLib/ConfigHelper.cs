#if BENCHMARK_OLD
using Config = ConfigCat.Client.SettingsWithPreferences;
#endif

namespace ConfigCat.Client.Tests.Helpers;

internal static class ConfigHelper
{
    public static Config FetchConfigCached(this ConfigLocation location) => location.FetchConfig();
}
