using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.ConfigService;

internal sealed class ManualPollConfigService : ConfigServiceBase, IConfigService
{
    internal ManualPollConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline = false, SafeHooksWrapper hooks = default)
        : base(configFetcher, cacheParameters, logger, isOffline, hooks)
    {
        var initialCacheSyncUpTask = SyncUpWithCacheAsync(WaitForReadyCancellationToken);
        ReadyTask = GetReadyTask(initialCacheSyncUpTask, async initialCacheSyncUpTask => GetCacheState(await initialCacheSyncUpTask.ConfigureAwait(false)));
    }

    public Task<ClientCacheState> ReadyTask { get; }

    public ProjectConfig GetConfig()
    {
        return this.ConfigCache.Get(base.CacheKey);
    }

    public ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        return this.ConfigCache.GetAsync(base.CacheKey, cancellationToken);
    }

    public override ClientCacheState GetCacheState(ProjectConfig cachedConfig)
    {
        if (cachedConfig.IsEmpty)
        {
            return ClientCacheState.NoFlagData;
        }

        return ClientCacheState.HasCachedFlagDataOnly;
    }
}
