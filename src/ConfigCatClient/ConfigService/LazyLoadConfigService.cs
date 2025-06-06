using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client.ConfigService;

internal sealed class LazyLoadConfigService : ConfigServiceBase, IConfigService
{
    private readonly TimeSpan cacheTimeToLive;

    internal LazyLoadConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, TimeSpan cacheTimeToLive, bool isOffline = false, SafeHooksWrapper hooks = default)
        : base(configFetcher, cacheParameters, logger, isOffline, hooks)
    {
        this.cacheTimeToLive = cacheTimeToLive;

        var initialCacheSyncUpTask = SyncUpWithCacheAsync().AsTask();
        ReadyTask = GetReadyTask(initialCacheSyncUpTask);
    }

    public Task<ClientCacheState> ReadyTask { get; }

    public ProjectConfig GetConfig()
    {
        var cachedConfig = SyncUpWithCache();

        if (cachedConfig.IsExpired(expiration: this.cacheTimeToLive))
        {
            if (!cachedConfig.IsEmpty)
            {
                OnConfigExpired();
            }

            if (!IsOffline)
            {
                var configWithFetchResult = RefreshConfigCore(cachedConfig, isInitiatedByUser: false);
                return configWithFetchResult.Item1;
            }
        }

        return cachedConfig;
    }

    public async ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var cachedConfig = await SyncUpWithCacheAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

        if (cachedConfig.IsExpired(expiration: this.cacheTimeToLive))
        {
            if (!cachedConfig.IsEmpty)
            {
                OnConfigExpired();
            }

            if (!IsOffline)
            {
                var configWithFetchResult = await RefreshConfigCoreAsync(cachedConfig, isInitiatedByUser: false, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                return configWithFetchResult.Item1;
            }
        }

        return cachedConfig;
    }

    private void OnConfigExpired()
    {
        this.Logger.Debug("config expired");
    }

    public override ClientCacheState GetCacheState(ProjectConfig cachedConfig)
    {
        if (cachedConfig.IsEmpty)
        {
            return ClientCacheState.NoFlagData;
        }

        if (cachedConfig.IsExpired(this.cacheTimeToLive))
        {
            return ClientCacheState.HasCachedFlagDataOnly;
        }

        return ClientCacheState.HasUpToDateFlagData;
    }
}
