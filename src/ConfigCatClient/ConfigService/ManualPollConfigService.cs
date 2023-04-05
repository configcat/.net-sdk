using System;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.ConfigService;

internal sealed class ManualPollConfigService : ConfigServiceBase, IConfigService
{
    internal ManualPollConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline = false, Hooks? hooks = null)
        : base(configFetcher, cacheParameters, logger, isOffline, hooks)
    {
        hooks?.RaiseClientReady();
    }

    public ProjectConfig GetConfig()
    {
        return this.ConfigCache.Get(base.CacheKey);
    }

    public async Task<ProjectConfig> GetConfigAsync()
    {
        return await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);
    }
}
