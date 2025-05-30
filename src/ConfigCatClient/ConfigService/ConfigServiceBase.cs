using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client.ConfigService;

using ConfigWithFetchResult = (ProjectConfig, FetchResult);

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

    private readonly CancellationTokenSource disposeTokenSource;
    protected CancellationToken DisposeToken => this.disposeTokenSource.Token;

    private Task<ProjectConfig>? pendingCacheSyncUp;
    private Task<ConfigWithFetchResult>? pendingConfigRefresh;

    protected ConfigServiceBase(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline, SafeHooksWrapper hooks)
    {
        this.ConfigFetcher = configFetcher;
        this.ConfigCache = cacheParameters.ConfigCache;
        this.CacheKey = cacheParameters.CacheKey;
        this.Logger = logger;
        this.Hooks = hooks;
        this.status = isOffline ? Status.Offline : Status.Online;
        this.disposeTokenSource = new CancellationTokenSource();
    }

    /// <remarks>
    /// Note for inheritors. Beware, this method is called within a lock statement.
    /// </remarks>
    protected virtual void DisposeSynchronized(bool disposing)
    {
        // Pending asynchronous operations (waiting for ready state, cache sync up, config refresh, etc.) should stop.
        this.disposeTokenSource.Cancel();

        if (disposing)
        {
            this.disposeTokenSource.Dispose();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            (this.ConfigFetcher as IDisposable)?.Dispose();
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

    public virtual async ValueTask<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default)
    {
        var latestConfig = await SyncUpWithCacheAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
        if (!IsOffline)
        {
            var (_, fetchResult) = await RefreshConfigCoreAsync(latestConfig, isInitiatedByUser: true, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            return RefreshResult.From(fetchResult);
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

    protected Task<ConfigWithFetchResult> RefreshConfigCoreAsync(ProjectConfig latestConfig, bool isInitiatedByUser, CancellationToken cancellationToken)
    {
        Task<ConfigWithFetchResult>? configRefreshTask;
        bool isInitiator;

        lock (this.ConfigFetcher)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // NOTE: Joiners may obtain more up-to-date config data from the external cache than the `latestConfig`
            // that was used to initiate the fetch operation. However, we ignore this possibility because we consider
            // the fetch operation result a more authentic source of truth. Although this may lead to overwriting
            // the cache with stale data, we expect this to be a temporary effect, which corrects itself eventually.

            configRefreshTask = this.pendingConfigRefresh;
            isInitiator = configRefreshTask is null or { IsCompleted: true };
            if (isInitiator)
            {
                this.pendingConfigRefresh = configRefreshTask = TaskShim.Current.Run(async () =>
                {
                    var fetchResult = await this.ConfigFetcher.FetchAsync(latestConfig, DisposeToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

                    var shouldUpdateCache =
                        fetchResult.IsSuccess
                        || fetchResult.IsNotModified
                        || fetchResult.Config.TimeStamp > latestConfig.TimeStamp // is not transient error?
                            && (!fetchResult.Config.IsEmpty || this.ConfigCache.LocalCachedConfig.IsEmpty);

                    if (shouldUpdateCache)
                    {
                        // NOTE: ExternalConfigCache.SetAsync makes sure that the external cache is not overwritten with empty
                        // config data under any circumstances.
                        await this.ConfigCache.SetAsync(this.CacheKey, fetchResult.Config, DisposeToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

                        latestConfig = fetchResult.Config;
                    }

                    OnConfigFetched(fetchResult, isInitiatedByUser);

                    if (fetchResult.IsSuccess)
                    {
                        OnConfigChanged(fetchResult.Config);
                    }

                    return new ConfigWithFetchResult(latestConfig, fetchResult);
                });
            }
        }

        if (isInitiator)
        {
            ScheduleCleanUp(configRefreshTask!);
        }

        return configRefreshTask!.WaitAsync(cancellationToken);

        async void ScheduleCleanUp(Task<ConfigWithFetchResult> refreshTask)
        {
            try { await refreshTask.ConfigureAwait(TaskShim.ContinueOnCapturedContext); }
            catch { /* Exceptions must not be allowed to bubble up. See also: https://stackoverflow.com/a/53266815/8656352 */ }
            finally
            {
                // NOTE: At this point the actual config refresh operation is completed, so there's no need to keep a
                // reference to the task any longer, but it should be set to null so GC can clean it up.
                // (Instead of locking, a call to Interlocked.CompareExchange is enough as .NET memory model guarantees
                // that both memory accesses to managed references and Interlocked methods are atomic operations.
                // See also: https://github.com/dotnet/runtime/blob/main/docs/design/specs/Memory-model.md#atomic-memory-accesses)
                _ = Interlocked.CompareExchange(ref this.pendingConfigRefresh, value: null, comparand: refreshTask);
            }
        }
    }

    protected virtual void OnConfigFetched(in FetchResult fetchResult, bool isInitiatedByUser)
    {
        this.Logger.Debug("config fetched");

        this.Hooks.RaiseConfigFetched(RefreshResult.From(fetchResult), isInitiatedByUser);
    }

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

    protected ValueTask<ProjectConfig> SyncUpWithCacheAsync(CancellationToken cancellationToken = default)
    {
        // InMemoryConfigCache always executes synchronously, so we special-case it for better performance.
        if (this.ConfigCache is InMemoryConfigCache inMemoryConfigCache)
        {
            var cacheGetTask = inMemoryConfigCache.GetAsync(this.CacheKey, cancellationToken);
            Debug.Assert(cacheGetTask.IsCompleted);
            var syncResult = cacheGetTask.GetAwaiter().GetResult();
            Debug.Assert(!syncResult.HasChanged);
            return new ValueTask<ProjectConfig>(syncResult.Config);
        }

        // Otherwise we join the pending cache sync up operation, or start a new one if there's none currently.
        Task<ProjectConfig>? cacheSyncUpTask = null;
        TaskCompletionSource<ProjectConfig>? cacheSyncUpTcs = null;
        Exception? exception = null;

        try
        {
            // NOTE: We want to start the cache sync operation directly on the current thread for performance and
            // backward compatibility reasons. However, this involves calling user-provided code
            // (IConfigCatCache.GetAsync), which may have a long-running initial synchronous part, or may not be
            // actual async code at all but a long-running synchronous operation returning a completed task.
            // Thus, we shouldn't directly start the cache sync operation within a lock.
            // Instead, we create a TaskCompletionSource to proxy the operation.

            lock (this.ConfigCache)
            {
                cancellationToken.ThrowIfCancellationRequested();

                cacheSyncUpTask = this.pendingCacheSyncUp;
                if (cacheSyncUpTask is null or { IsCompleted: true })
                {
                    cacheSyncUpTcs = TaskShim.CreateSafeCompletionSource<ProjectConfig>();
                    this.pendingCacheSyncUp = cacheSyncUpTask = cacheSyncUpTcs.Task;
                }
            }

            if (cacheSyncUpTcs is not null) // is this thread the initiator of the operation?
            {
                var cacheGetTask = this.ConfigCache.GetAsync(this.CacheKey, DisposeToken);
                if (cacheGetTask.IsCompleted)
                {
                    // If the user-provided cache implementation is actually synchronous, let's avoid the cost of the
                    // async state machine. (See also the advanced pattern in
                    // https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/)
                    var syncResult = cacheGetTask.GetAwaiter().GetResult();
                    OnCacheSynced(syncResult);
                    cacheSyncUpTcs.TrySetResult(syncResult.Config);
                    _ = Interlocked.CompareExchange(ref this.pendingCacheSyncUp, value: null, comparand: cacheSyncUpTask);
                }
                else
                {
                    AwaitAndCleanUp(cacheGetTask, cacheSyncUpTcs, cacheSyncUpTask!);
                }
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

        async void AwaitAndCleanUp(ValueTask<CacheSyncResult> cacheGetTask, TaskCompletionSource<ProjectConfig> cacheSyncUpTcs, Task<ProjectConfig> cacheSyncUpTask)
        {
            try
            {
                try
                {
                    var syncResult = await cacheGetTask.ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                    OnCacheSynced(syncResult);
                    cacheSyncUpTcs.TrySetResult(syncResult.Config);
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

    private void OnCacheSynced(CacheSyncResult syncResult)
    {
        if (syncResult.HasChanged && !syncResult.Config.IsEmpty)
        {
            OnConfigChanged(syncResult.Config);
        }
    }

    protected virtual async ValueTask<ClientCacheState> WaitForReadyAsync(Task<ProjectConfig> initialCacheSyncUpTask)
    {
        return GetCacheState(await initialCacheSyncUpTask.ConfigureAwait(TaskShim.ContinueOnCapturedContext));
    }

    protected async Task<ClientCacheState> GetReadyTask(Task<ProjectConfig> initialCacheSyncUpTask)
    {
        var cacheState = await WaitForReadyAsync(initialCacheSyncUpTask).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
        this.Hooks.RaiseClientReady(cacheState);
        return cacheState;
    }
}
