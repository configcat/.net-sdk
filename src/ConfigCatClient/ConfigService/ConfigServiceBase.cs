using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Shims;

#if NET45
using ConfigWithFetchResult = System.Tuple<ConfigCat.Client.ProjectConfig, ConfigCat.Client.FetchResult>;
#else
using ConfigWithFetchResult = System.ValueTuple<ConfigCat.Client.ProjectConfig, ConfigCat.Client.FetchResult>;
#endif

namespace ConfigCat.Client.ConfigService;

internal abstract class ConfigServiceBase : IDisposable
{
    protected internal enum Status
    {
        Online,
        Offline,
        Disposed,
    }

    private Status status;
    private readonly object syncObj = new();

    protected readonly IConfigFetcher ConfigFetcher;
    protected readonly ConfigCache ConfigCache;
    protected readonly LoggerWrapper Logger;
    protected readonly string CacheKey;
    protected readonly SafeHooksWrapper Hooks;

    private CancellationTokenSource? waitForReadyCancellationTokenSource;
    protected CancellationToken WaitForReadyCancellationToken => this.waitForReadyCancellationTokenSource?.Token ?? new CancellationToken(canceled: true);

    protected ConfigServiceBase(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline, SafeHooksWrapper hooks)
    {
        this.ConfigFetcher = configFetcher;
        this.ConfigCache = cacheParameters.ConfigCache;
        this.CacheKey = cacheParameters.CacheKey;
        this.Logger = logger;
        this.Hooks = hooks;
        this.status = isOffline ? Status.Offline : Status.Online;
        this.waitForReadyCancellationTokenSource = new CancellationTokenSource();
    }

    /// <remarks>
    /// Note for inheritors. Beware, this method is called within a lock statement.
    /// </remarks>
    protected virtual void DisposeSynchronized(bool disposing)
    {
        // If waiting for ready state is still in progress, it should stop.
        this.waitForReadyCancellationTokenSource?.Cancel();

        if (disposing)
        {
            this.waitForReadyCancellationTokenSource?.Dispose();
            this.waitForReadyCancellationTokenSource = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && this.ConfigFetcher is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public void Dispose()
    {
        lock (this.syncObj)
        {
            if (this.status == Status.Disposed)
            {
                return;
            }

            this.status = Status.Disposed;

            DisposeSynchronized(true);
        }

        Dispose(true);
    }

    public ProjectConfig GetInMemoryConfig() => this.ConfigCache.LocalCachedConfig;

    public virtual RefreshResult RefreshConfig()
    {
        if (!IsOffline)
        {
            var latestConfig = this.ConfigCache.Get(this.CacheKey);
            var configWithFetchResult = RefreshConfigCore(latestConfig, isInitiatedByUser: true);
            return RefreshResult.From(configWithFetchResult.Item2);
        }
        else
        {
            var logMessage = this.Logger.ConfigServiceCannotInitiateHttpCalls();
            return RefreshResult.Failure(RefreshErrorCode.OfflineClient, logMessage.ToLazyString());
        }
    }

    protected ConfigWithFetchResult RefreshConfigCore(ProjectConfig latestConfig, bool isInitiatedByUser)
    {
        var fetchResult = this.ConfigFetcher.Fetch(latestConfig);

        if (fetchResult.IsSuccess
            || fetchResult.Config.TimeStamp > latestConfig.TimeStamp && (!fetchResult.Config.IsEmpty || latestConfig.IsEmpty))
        {
            this.ConfigCache.Set(this.CacheKey, fetchResult.Config);

            latestConfig = fetchResult.Config;
        }

        OnConfigFetched(fetchResult, isInitiatedByUser);

        if (fetchResult.IsSuccess)
        {
            OnConfigChanged(fetchResult);
        }

        return new ConfigWithFetchResult(latestConfig, fetchResult);
    }

    public virtual async ValueTask<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default)
    {
        if (!IsOffline)
        {
            var latestConfig = await this.ConfigCache.GetAsync(this.CacheKey, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            var configWithFetchResult = await RefreshConfigCoreAsync(latestConfig, isInitiatedByUser: true, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            return RefreshResult.From(configWithFetchResult.Item2);
        }
        else
        {
            var logMessage = this.Logger.ConfigServiceCannotInitiateHttpCalls();
            return RefreshResult.Failure(RefreshErrorCode.OfflineClient, logMessage.ToLazyString());
        }
    }

    protected async Task<ConfigWithFetchResult> RefreshConfigCoreAsync(ProjectConfig latestConfig, bool isInitiatedByUser, CancellationToken cancellationToken)
    {
        var fetchResult = await this.ConfigFetcher.FetchAsync(latestConfig, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

        if (fetchResult.IsSuccess
            || fetchResult.Config.TimeStamp > latestConfig.TimeStamp && (!fetchResult.Config.IsEmpty || latestConfig.IsEmpty))
        {
            await this.ConfigCache.SetAsync(this.CacheKey, fetchResult.Config, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            latestConfig = fetchResult.Config;
        }

        OnConfigFetched(fetchResult, isInitiatedByUser);

        if (fetchResult.IsSuccess)
        {
            OnConfigChanged(fetchResult);
        }

        return new ConfigWithFetchResult(latestConfig, fetchResult);
    }

    protected virtual void OnConfigFetched(in FetchResult fetchResult, bool isInitiatedByUser)
    {
        this.Logger.Debug("config fetched");

        this.Hooks.RaiseConfigFetched(RefreshResult.From(fetchResult), isInitiatedByUser);
    }

    protected virtual void OnConfigChanged(in FetchResult fetchResult)
    {
        this.Logger.Debug("config changed");

        this.Hooks.RaiseConfigChanged(fetchResult.Config.Config ?? new Config());
    }

    public bool IsOffline
    {
        get
        {
            lock (this.syncObj)
            {
                return this.status != Status.Online;
            }
        }
    }

    /// <remarks>
    /// Note for inheritors. Beware, this method is called within a lock statement.
    /// </remarks>
    protected virtual void SetOnlineCoreSynchronized() { }

    public void SetOnline()
    {
        Action<LoggerWrapper>? logAction = null;

        lock (this.syncObj)
        {
            if (this.status == Status.Offline)
            {
                SetOnlineCoreSynchronized();
                this.status = Status.Online;
                logAction = static logger => logger.ConfigServiceStatusChanged(Status.Online);
            }
            else if (this.status == Status.Disposed)
            {
                logAction = static logger => logger.ConfigServiceMethodHasNoEffectDueToDisposedClient(nameof(SetOnline));
            }
        }

        logAction?.Invoke(this.Logger);
    }

    /// <remarks>
    /// Note for inheritors. Beware, this method is called within a lock statement.
    /// </remarks>
    protected virtual void SetOfflineCoreSynchronized() { }

    public void SetOffline()
    {
        Action<LoggerWrapper>? logAction = null;

        lock (this.syncObj)
        {
            if (this.status == Status.Online)
            {
                SetOfflineCoreSynchronized();
                this.status = Status.Offline;
                logAction = static logger => logger.ConfigServiceStatusChanged(Status.Offline);
            }
            else if (this.status == Status.Disposed)
            {
                logAction = static logger => logger.ConfigServiceMethodHasNoEffectDueToDisposedClient(nameof(SetOffline));
            }
        }

        logAction?.Invoke(this.Logger);
    }

    public abstract ClientCacheState GetCacheState(ProjectConfig cachedConfig);

    protected Task<ProjectConfig> SyncUpWithCacheAsync(CancellationToken cancellationToken)
    {
        return this.ConfigCache.GetAsync(this.CacheKey, cancellationToken).AsTask();
    }

    protected virtual async ValueTask<ClientCacheState> WaitForReadyAsync(Task<ProjectConfig> initialCacheSyncUpTask)
    {
        return GetCacheState(await initialCacheSyncUpTask.ConfigureAwait(TaskShim.ContinueOnCapturedContext));
    }

    protected async Task<ClientCacheState> GetReadyTask(Task<ProjectConfig> initialCacheSyncUpTask)
    {
        ClientCacheState cacheState;
        try { cacheState = await WaitForReadyAsync(initialCacheSyncUpTask).ConfigureAwait(TaskShim.ContinueOnCapturedContext); }
        finally
        {
            lock (this.syncObj)
            {
                this.waitForReadyCancellationTokenSource?.Dispose();
                this.waitForReadyCancellationTokenSource = null;
            }
        }

        this.Hooks.RaiseClientReady(cacheState);

        return cacheState;
    }
}
