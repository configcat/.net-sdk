using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client.ConfigService;

internal sealed class AutoPollConfigService : ConfigServiceBase, IConfigService
{
    private readonly TimeSpan pollInterval;
    private readonly TimeSpan maxInitWaitTime;
    private readonly CancellationTokenSource initSignalCancellationTokenSource = new(); // used for signalling initialization ready
    private CancellationTokenSource timerCancellationTokenSource = new(); // used for signalling background work to stop

    internal AutoPollConfigService(
        AutoPoll options,
        IConfigFetcher configFetcher,
        CacheParameters cacheParameters,
        LoggerWrapper logger,
        bool isOffline = false,
        SafeHooksWrapper hooks = default) : this(options, configFetcher, cacheParameters, logger, startTimer: true, isOffline, hooks)
    { }

    // For test purposes only
    internal AutoPollConfigService(
        AutoPoll options,
        IConfigFetcher configFetcher,
        CacheParameters cacheParameters,
        LoggerWrapper logger,
        bool startTimer,
        bool isOffline = false,
        SafeHooksWrapper hooks = default) : base(configFetcher, cacheParameters, logger, isOffline, hooks)
    {
        this.pollInterval = options.PollInterval;
        this.maxInitWaitTime = options.MaxInitWaitTime >= TimeSpan.Zero ? options.MaxInitWaitTime : Timeout.InfiniteTimeSpan;

        var initialCacheSyncUpTask = SyncUpWithCacheAsync(WaitForReadyCancellationToken);

        // This task will complete when either initalization ready is signalled by cancelling initializationCancellationTokenSource or maxInitWaitTime passes.
        // If the service gets disposed before any of these events happen, the task will also complete, but with a canceled status.
        InitializationTask = WaitForInitializationAsync(WaitForReadyCancellationToken);

        ReadyTask = GetReadyTask(InitializationTask, async initializationTask =>
        {
            // In Auto Polling mode, maxInitWaitTime takes precedence over waiting for initial cache sync-up, that is,
            // ClientReady is always raised after maxInitWaitTime has passed, regardless of whether initial cache sync-up has finished or not.
            await initializationTask.ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            return GetCacheState(this.ConfigCache.LocalCachedConfig);
        });

        if (!isOffline && startTimer)
        {
            StartScheduler(initialCacheSyncUpTask, this.timerCancellationTokenSource.Token);
        }
    }

    internal Task<bool> InitializationTask { get; }

    public Task<ClientCacheState> ReadyTask { get; }

    protected override void DisposeSynchronized(bool disposing)
    {
        // Background work should stop under all circumstances
        this.timerCancellationTokenSource.Cancel();

        if (disposing)
        {
            this.timerCancellationTokenSource.Dispose();
        }

        base.DisposeSynchronized(disposing);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.initSignalCancellationTokenSource.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool IsInitialized => InitializationTask.Status == TaskStatus.RanToCompletion;

    private void SignalInitialization()
    {
        try
        {
            if (!this.initSignalCancellationTokenSource.IsCancellationRequested)
            {
                this.initSignalCancellationTokenSource.Cancel();
                this.initSignalCancellationTokenSource.Dispose();
            }
        }
        catch (ObjectDisposedException)
        {
            // Since SignalInitialization and Dispose are not synchronized,
            // in extreme conditions a call to SignalInitialization may slip past the disposal of initializationCancellationTokenSource.
            // In such cases we get an ObjectDisposedException here, which means that the config service has been disposed in the meantime.
            // Thus, we can safely swallow this exception.
        }
    }

    private async Task<bool> WaitForInitializationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TaskShim.Current.Delay(this.maxInitWaitTime, this.initSignalCancellationTokenSource.Token)
                .WaitAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            return false;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            return true;
        }
    }

    public ProjectConfig GetConfig()
    {
        if (!IsOffline && !IsInitialized)
        {
            var cachedConfig = this.ConfigCache.Get(base.CacheKey);
            if (!cachedConfig.IsExpired(expiration: this.pollInterval))
            {
                return cachedConfig;
            }

            // NOTE: We go sync over async here, however it's safe to do that in this case as
            // the task will be completed on a thread pool thread (either by the polling loop or a timer callback).
            InitializationTask.GetAwaiter().GetResult();
        }

        return this.ConfigCache.Get(base.CacheKey);
    }

    public async ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        if (!IsOffline && !IsInitialized)
        {
            var cachedConfig = await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            if (!cachedConfig.IsExpired(expiration: this.pollInterval))
            {
                return cachedConfig;
            }

            await InitializationTask.WaitAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
        }

        return await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
    }

    protected override void OnConfigFetched(in FetchResult fetchResult, bool isInitiatedByUser)
    {
        SignalInitialization();
        base.OnConfigFetched(fetchResult, isInitiatedByUser);
    }

    protected override void SetOnlineCoreSynchronized()
    {
        StartScheduler(null, this.timerCancellationTokenSource.Token);
    }

    protected override void SetOfflineCoreSynchronized()
    {
        this.timerCancellationTokenSource.Cancel();
        this.timerCancellationTokenSource.Dispose();
        this.timerCancellationTokenSource = new CancellationTokenSource();
    }

    private void StartScheduler(Task<ProjectConfig>? initialCacheSyncUpTask, CancellationToken stopToken)
    {
        TaskShim.Current.Run(async () =>
        {
            var isFirstIteration = true;

            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    var scheduledNextTime = DateTime.UtcNow.Add(this.pollInterval);
                    try
                    {
                        await PollCoreAsync(isFirstIteration, initialCacheSyncUpTask, stopToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        this.Logger.AutoPollConfigServiceErrorDuringPolling(ex);
                    }

                    var realNextTime = scheduledNextTime.Subtract(DateTime.UtcNow);
                    if (realNextTime > TimeSpan.Zero)
                    {
                        await TaskShim.Current.Delay(realNextTime, stopToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore exceptions from cancellation.
                }
                catch (Exception ex)
                {
                    this.Logger.AutoPollConfigServiceErrorDuringPolling(ex);
                }

                isFirstIteration = false;
                initialCacheSyncUpTask = null; // allow GC to collect the task and its result
            }

            return default(object);
        }, stopToken);
    }

    private async ValueTask PollCoreAsync(bool isFirstIteration, Task<ProjectConfig>? initialCacheSyncUpTask, CancellationToken cancellationToken)
    {
        if (isFirstIteration)
        {
            var latestConfig = initialCacheSyncUpTask is not null
                ? await initialCacheSyncUpTask.WaitAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext)
                : await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            if (latestConfig.IsExpired(expiration: this.pollInterval))
            {
                if (!IsOffline)
                {
                    await RefreshConfigCoreAsync(latestConfig, isInitiatedByUser: false, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                }
            }
            else
            {
                SignalInitialization();
            }
        }
        else
        {
            if (!IsOffline)
            {
                var latestConfig = await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                await RefreshConfigCoreAsync(latestConfig, isInitiatedByUser: false, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            }
        }
    }

    public override ClientCacheState GetCacheState(ProjectConfig cachedConfig)
    {
        if (cachedConfig.IsEmpty)
        {
            return ClientCacheState.NoFlagData;
        }

        if (cachedConfig.IsExpired(this.pollInterval))
        {
            return ClientCacheState.HasCachedFlagDataOnly;
        }

        return ClientCacheState.HasUpToDateFlagData;
    }
}
