using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client.ConfigService;

internal sealed class ManualPollConfigService : ConfigServiceBase, IConfigService
{
    internal ManualPollConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline = false, SafeHooksWrapper hooks = default)
        : base(configFetcher, cacheParameters, logger, isOffline, hooks)
    {
        PrepareClientForEvents(this, hooks);

        var initialCacheSyncUpTask = SyncUpWithCacheAsync().AsTask();
        ReadyTask = GetReadyTask(initialCacheSyncUpTask);
    }

    public Task<ClientCacheState> ReadyTask { get; }

    public ProjectConfig GetConfig()
    {
        return SyncUpWithCache();
    }

    public async ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        return await SyncUpWithCacheAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
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
