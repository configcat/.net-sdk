using System;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Utils;

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

#pragma warning disable CS0618 // Type or member is obsolete
    protected readonly IConfigCache ConfigCache; // Backward compatibility, it'll be changed to IConfigCatCache later.
#pragma warning restore CS0618 // Type or member is obsolete

    protected readonly LoggerWrapper Logger;
    protected readonly string CacheKey;
    protected readonly Hooks Hooks;

    protected ConfigServiceBase(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline, Hooks hooks)
    {
        this.ConfigFetcher = configFetcher;
        this.ConfigCache = cacheParameters.ConfigCache;
        this.CacheKey = cacheParameters.CacheKey;
        this.Logger = logger;
        this.Hooks = hooks ?? NullHooks.Instance;
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
        // check for the new cache interface until we remove the old IConfigCache.
        if (this.ConfigCache is IConfigCatCache cache)
        {
            if (!IsOffline)
            {
                var latestConfig = cache.Get(this.CacheKey);
                var configWithFetchResult = RefreshConfigCore(latestConfig);
                return RefreshResult.From(configWithFetchResult.Item2);
            }
            else
            {
                this.Logger.ConfigServiceCantInitiateHttpCalls();
                return RefreshResult.Failure("Client is in offline mode, it can't initiate HTTP calls.");
            }
        }

        // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
        return Syncer.Sync(RefreshConfigAsync);
    }

    protected ConfigWithFetchResult RefreshConfigCore(ProjectConfig latestConfig)
    {
        var fetchResult = this.ConfigFetcher.Fetch(latestConfig);
        var newConfig = fetchResult.Config;

        var configContentHasChanged = !ProjectConfig.ContentEquals(latestConfig, newConfig);
        if ((configContentHasChanged || newConfig.TimeStamp > latestConfig.TimeStamp) && !newConfig.IsEmpty)
        {
            // TODO: This cast can be removed when we delete the obsolete IConfigCache interface.
            ((IConfigCatCache)this.ConfigCache).Set(this.CacheKey, newConfig);

            OnConfigUpdated(newConfig);

            if (configContentHasChanged)
            {
                OnConfigChanged(newConfig);
            }

            return new ConfigWithFetchResult(newConfig, fetchResult);
        }

        return new ConfigWithFetchResult(latestConfig, fetchResult);
    }

    public virtual async Task<RefreshResult> RefreshConfigAsync()
    {
        if (!IsOffline)
        {
            var latestConfig = await this.ConfigCache.GetAsync(this.CacheKey).ConfigureAwait(false);
            var configWithFetchResult = await RefreshConfigCoreAsync(latestConfig).ConfigureAwait(false);
            return RefreshResult.From(configWithFetchResult.Item2);
        }
        else
        {
            this.Logger.ConfigServiceCantInitiateHttpCalls();
            return RefreshResult.Failure("Client is in offline mode, it can't initiate HTTP calls.");
        }
    }

    protected async Task<ConfigWithFetchResult> RefreshConfigCoreAsync(ProjectConfig latestConfig)
    {
        var fetchResult = await this.ConfigFetcher.FetchAsync(latestConfig).ConfigureAwait(false);
        var newConfig = fetchResult.Config;

        var configContentHasChanged = !ProjectConfig.ContentEquals(latestConfig, newConfig);
        if ((configContentHasChanged || newConfig.TimeStamp > latestConfig.TimeStamp) && !newConfig.IsEmpty)
        {
            await this.ConfigCache.SetAsync(this.CacheKey, newConfig).ConfigureAwait(false);

            OnConfigUpdated(newConfig);

            if (configContentHasChanged)
            {
                OnConfigChanged(newConfig);
            }

            return new ConfigWithFetchResult(newConfig, fetchResult);
        }

        return new ConfigWithFetchResult(latestConfig, fetchResult);
    }

    protected virtual void OnConfigUpdated(ProjectConfig newConfig) { }

    protected virtual void OnConfigChanged(ProjectConfig newConfig)
    {
        this.Logger.Debug("config changed");

        this.Hooks.RaiseConfigChanged(newConfig);
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
        Action<ILogger> logAction = null;

        lock (this.syncObj)
        {
            if (this.status == Status.Offline)
            {
                SetOnlineCoreSynchronized();
                this.status = Status.Online;
                logAction = static logger => logger.ConfigServiceStatusChange(Status.Online);
            }
            else if (this.status == Status.Disposed)
            {
                logAction = static logger => logger.ConfigServiceMethodHasNoEffect(nameof(SetOnline));
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
        Action<ILogger> logAction = null;

        lock (this.syncObj)
        {
            if (this.status == Status.Online)
            {
                SetOfflineCoreSynchronized();
                this.status = Status.Offline;
                logAction = static logger => logger.ConfigServiceStatusChange(Status.Offline);
            }
            else if (this.status == Status.Disposed)
            {
                logAction = static logger => logger.ConfigServiceMethodHasNoEffect(nameof(SetOffline));
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
