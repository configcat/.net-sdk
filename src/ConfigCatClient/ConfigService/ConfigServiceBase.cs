using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;

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

    protected ConfigServiceBase(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline, SafeHooksWrapper hooks)
    {
        this.ConfigFetcher = configFetcher;
        this.ConfigCache = cacheParameters.ConfigCache;
        this.CacheKey = cacheParameters.CacheKey;
        this.Logger = logger;
        this.Hooks = hooks;
        this.status = isOffline ? Status.Offline : Status.Online;
    }

    /// <remarks>
    /// Note for inheritors. Beware, this method is called within a lock statement.
    /// </remarks>
    protected virtual void DisposeSynchronized(bool disposing) { }

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

    public virtual RefreshResult RefreshConfig()
    {
        if (!IsOffline)
        {
            var latestConfig = this.ConfigCache.Get(this.CacheKey);
            var configWithFetchResult = RefreshConfigCore(latestConfig);
            return RefreshResult.From(configWithFetchResult.Item2);
        }
        else
        {
            var logMessage = this.Logger.ConfigServiceCannotInitiateHttpCalls();
            return RefreshResult.Failure(RefreshErrorCode.OfflineClient, logMessage.InvariantFormattedMessage);
        }
    }

    protected ConfigWithFetchResult RefreshConfigCore(ProjectConfig latestConfig)
    {
        var fetchResult = this.ConfigFetcher.Fetch(latestConfig);

        if (fetchResult.IsSuccess
            || fetchResult.Config.TimeStamp > latestConfig.TimeStamp && (!fetchResult.Config.IsEmpty || latestConfig.IsEmpty))
        {
            this.ConfigCache.Set(this.CacheKey, fetchResult.Config);

            latestConfig = fetchResult.Config;
        }

        OnConfigFetched(fetchResult.Config);

        if (fetchResult.IsSuccess)
        {
            OnConfigChanged(fetchResult.Config);
        }

        return new ConfigWithFetchResult(latestConfig, fetchResult);
    }

    public virtual async ValueTask<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default)
    {
        if (!IsOffline)
        {
            var latestConfig = await this.ConfigCache.GetAsync(this.CacheKey, cancellationToken).ConfigureAwait(false);
            var configWithFetchResult = await RefreshConfigCoreAsync(latestConfig, cancellationToken).ConfigureAwait(false);
            return RefreshResult.From(configWithFetchResult.Item2);
        }
        else
        {
            var logMessage = this.Logger.ConfigServiceCannotInitiateHttpCalls();
            return RefreshResult.Failure(RefreshErrorCode.OfflineClient, logMessage.InvariantFormattedMessage);
        }
    }

    protected async Task<ConfigWithFetchResult> RefreshConfigCoreAsync(ProjectConfig latestConfig, CancellationToken cancellationToken)
    {
        var fetchResult = await this.ConfigFetcher.FetchAsync(latestConfig, cancellationToken).ConfigureAwait(false);

        if (fetchResult.IsSuccess
            || fetchResult.Config.TimeStamp > latestConfig.TimeStamp && (!fetchResult.Config.IsEmpty || latestConfig.IsEmpty))
        {
            await this.ConfigCache.SetAsync(this.CacheKey, fetchResult.Config, cancellationToken).ConfigureAwait(false);

            latestConfig = fetchResult.Config;
        }

        OnConfigFetched(fetchResult.Config);

        if (fetchResult.IsSuccess)
        {
            OnConfigChanged(fetchResult.Config);
        }

        return new ConfigWithFetchResult(latestConfig, fetchResult);
    }

    protected virtual void OnConfigFetched(ProjectConfig newConfig) { }

    protected virtual void OnConfigChanged(ProjectConfig newConfig)
    {
        this.Logger.Debug("config changed");

        this.Hooks.RaiseConfigChanged(newConfig.Config ?? new Config());
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

    protected TResult Synchronize<TState, TResult>(Func<TState, TResult> func, TState state)
    {
        lock (this.syncObj)
        {
            return func(state);
        }
    }
}
