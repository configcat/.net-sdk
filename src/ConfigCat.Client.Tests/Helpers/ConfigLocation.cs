#if BENCHMARK_OLD
using Config = ConfigCat.Client.SettingsWithPreferences;
#endif

namespace ConfigCat.Client.Tests.Helpers;

public abstract partial record class ConfigLocation
{
    private ConfigLocation() { }

    public abstract string GetRealLocation();

    internal abstract Config FetchConfig();
}
