using System;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.ConfigService;

internal sealed class ManualPollConfigService : ConfigServiceBase, IConfigService
{
    internal ManualPollConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline = false, Hooks hooks = null)
        : base(configFetcher, cacheParameters, logger, isOffline, hooks)
    {
        hooks?.RaiseClientReady();
    }

    public ProjectConfig GetConfig()
    {
        // check for the new cache interface until we remove the old IConfigCache.
        if (this.ConfigCache is IConfigCatCache cache)
        {
            return cache.Get(base.CacheKey);
        }

        // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
        return Syncer.Sync(GetConfigAsync);
    }

    public async Task<ProjectConfig> GetConfigAsync()
    {
        return await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);
    }
}
