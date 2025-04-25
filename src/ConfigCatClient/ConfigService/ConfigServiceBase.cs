using System;
using System.Diagnostics;
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

    private Task<ProjectConfig>? pendingCacheSyncUp;

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
        var latestConfig = SyncUpWithCache();
        if (!IsOffline)
        {
            var configWithFetchResult = RefreshConfigCore(latestConfig, isInitiatedByUser: true);
            return RefreshResult.From(configWithFetchResult.Item2);
        }
        else if (this.ConfigCache is ExternalConfigCache)
        {
            return RefreshResult.Success();
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
        var latestConfig = await SyncUpWithCacheAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
        if (!IsOffline)
        {
            var configWithFetchResult = await RefreshConfigCoreAsync(latestConfig, isInitiatedByUser: true, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            return RefreshResult.From(configWithFetchResult.Item2);
        }
        else if (this.ConfigCache is ExternalConfigCache)
        {
            return RefreshResult.Success();
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
    protected virtual void GoOnlineSynchronized() { }

    public void SetOnline()
    {
        Action<LoggerWrapper>? logAction = null;

        lock (this.syncObj)
        {
            if (this.status == Status.Offline)
            {
                GoOnlineSynchronized();
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

    public void SetOffline()
    {
        Action<LoggerWrapper>? logAction = null;

        lock (this.syncObj)
        {
            if (this.status == Status.Online)
            {
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

    protected ProjectConfig SyncUpWithCache()
    {
        // NOTE: We don't try to join concurrent asynchronous cache synchronization because it would be hard, if not
        // impossible to do that in a deadlock-free way. Plus, the synchronous code paths will be deleted soon anyway.
        return this.ConfigCache.Get(this.CacheKey);
    }

    protected ValueTask<ProjectConfig> SyncUpWithCacheAsync(CancellationToken cancellationToken)
    {
        // InMemoryConfigCache always executes synchronously, so we special-case it for better performance.
        if (this.ConfigCache is InMemoryConfigCache inMemoryConfigCache)
        {
            var syncResultTask = inMemoryConfigCache.GetAsync(this.CacheKey, cancellationToken);
            Debug.Assert(syncResultTask.IsCompleted);
            return syncResultTask;
        }

        // Otherwise we join the pending cache sync up operation, or start a new one if there's none currently.
        Task<ProjectConfig>? cacheSyncUpTask = null;
        TaskCompletionSource<ProjectConfig>? cacheSyncUpTcs = null;
        Exception? exception = null;

        try
        {
            // NOTE: We want to start the cache sync operation directly on the current thread for performance and
            // backward compatibility reasons. However, this involves calling user-provided code
            // (IConfigCatCache.GetAsync), which, if implemented incorrectly, may have a long-running initial
            // synchronous part, or may not be actual async code at all but a long-running synchronous operation
            // returning a completed task. Thus, we shouldn't directly start the cache sync operation within a lock.
            // Instead, we create a TaskCompletionSource to proxy the operation.

            lock (this.ConfigCache)
            {
                cancellationToken.ThrowIfCancellationRequested();

                cacheSyncUpTask = this.pendingCacheSyncUp;
                if (cacheSyncUpTask is null or { IsCompleted: true })
                {
                    cacheSyncUpTcs = TaskShim.CreateSafeCompletionSource(out cacheSyncUpTask);
                    this.pendingCacheSyncUp = cacheSyncUpTask;
                }
            }

            if (cacheSyncUpTcs is not null) // is this thread the initiator of the operation?
            {
                ExecuteOperationAndCleanUp(cacheSyncUpTcs, cacheSyncUpTask!);
            }
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            if (cacheSyncUpTcs is not null)
            {
                // If anything goes wrong, we need to make sure that this.pendingCacheSyncUp is completed to not get stuck with it indefinitely.
#if !NETCOREAPP
                if (exception is not null
                    // NOTE: Thread.Abort is also possible on older runtimes, but it can't avoid or interrupt finally blocks,
                    // this is why we do these checks here (see also https://learn.microsoft.com/en-us/dotnet/standard/threading/destroying-threads).
                    || (Thread.CurrentThread.ThreadState & System.Threading.ThreadState.AbortRequested) != 0)
                {
                    try { _ = exception is not null ? cacheSyncUpTcs.TrySetException(exception) : cacheSyncUpTcs.TrySetCanceled(); }
#else
                if (exception is not null)
                {
                    try { cacheSyncUpTcs.TrySetException(exception); }
#endif
                    finally { _ = Interlocked.CompareExchange(ref this.pendingCacheSyncUp, value: null, comparand: cacheSyncUpTask); }
                }
            }
        }

        return new ValueTask<ProjectConfig>(cacheSyncUpTask.WaitAsync(cancellationToken));

        async void ExecuteOperationAndCleanUp(TaskCompletionSource<ProjectConfig> cacheSyncUpTcs, Task<ProjectConfig> cacheSyncUpTask)
        {
            try
            {
                try
                {
                    var cachedConfig = await this.ConfigCache.GetAsync(this.CacheKey).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                    cacheSyncUpTcs.TrySetResult(cachedConfig);
                }
                catch (OperationCanceledException ex)
                {
                    cacheSyncUpTcs.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    cacheSyncUpTcs.TrySetException(ex);
                }
            }
            catch { /* Exceptions must not be allowed to bubble up. See also: https://stackoverflow.com/a/53266815/8656352 */ }
            finally
            {
                // NOTE: At this point the actual cache sync operation is completed, so there's no need to keep a
                // reference to the task any longer, but it should be set to null so GC can clean it up.
                _ = Interlocked.CompareExchange(ref this.pendingCacheSyncUp, value: null, comparand: cacheSyncUpTask);
            }
        }
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
