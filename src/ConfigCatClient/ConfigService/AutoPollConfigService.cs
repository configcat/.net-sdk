using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Shims;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.ConfigService;

internal sealed class AutoPollConfigService : ConfigServiceBase, IConfigService
{
    internal const int PollExpirationToleranceMs = 500;

    private readonly TimeSpan pollInterval;
    private readonly TimeSpan pollExpiration;
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
        // Due to the inaccuracy of the timer, some tolerance should be allowed when checking for
        // cache expiration in the polling loop, otherwise some fetch operations may be missed.
        this.pollExpiration = options.PollInterval - TimeSpan.FromMilliseconds(PollExpirationToleranceMs);

        this.maxInitWaitTime = options.MaxInitWaitTime >= TimeSpan.Zero ? options.MaxInitWaitTime : Timeout.InfiniteTimeSpan;

        var initialCacheSyncUpTask = SyncUpWithCacheAsync(WaitForReadyCancellationToken);

        // This task will complete as soon as
        // 1. a cache sync operation completes, and the obtained config is up-to-date (see GetConfig/GetConfigAsync and PollCoreAsync),
        // 2. or, in case the client is online and the local cache is still empty or expired after the initial cache sync-up,
        //    the first config fetch operation completes, regardless of success or failure (see OnConfigFetched).
        // If the service gets disposed before any of these events happen, the task will also complete, but with a canceled status.
        InitializationTask = WaitForInitializationAsync(WaitForReadyCancellationToken);

        // In Auto Polling mode, maxInitWaitTime takes precedence over waiting for initial cache sync-up, that is,
        // ClientReady is always raised after maxInitWaitTime has passed, regardless of whether initial cache sync-up has finished or not.
        ReadyTask = GetReadyTask(initialCacheSyncUpTask);

        if (startTimer)
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

    protected override async ValueTask<ClientCacheState> WaitForReadyAsync(Task<ProjectConfig> initialCacheSyncUpTask)
    {
        await InitializationTask.ConfigureAwait(TaskShim.ContinueOnCapturedContext);
        return GetCacheState(this.ConfigCache.LocalCachedConfig);
    }

    private async Task<bool> WaitForInitializationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TaskShim.Current.Delay(this.maxInitWaitTime, this.initSignalCancellationTokenSource.Token)
                .WaitAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return true;
        }
    }

    public ProjectConfig GetConfig()
    {
        var cachedConfig = this.ConfigCache.Get(base.CacheKey);

        if (!cachedConfig.IsExpired(expiration: this.pollInterval))
        {
            SignalInitialization();
        }
        else if (!IsOffline && !IsInitialized)
        {
            // NOTE: We go sync over async here, however it's safe to do that in this case as
            // the task will be completed on a thread pool thread (either by the polling loop or a timer callback).
            InitializationTask.GetAwaiter().GetResult();
            cachedConfig = this.ConfigCache.LocalCachedConfig;
        }

        return cachedConfig;
    }

    public async ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var cachedConfig = await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

        if (!cachedConfig.IsExpired(expiration: this.pollInterval))
        {
            SignalInitialization();
        }
        else if (!IsOffline && !IsInitialized)
        {
            await InitializationTask.WaitAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            cachedConfig = this.ConfigCache.LocalCachedConfig;
        }

        return cachedConfig;
    }

    protected override void OnConfigFetched(in FetchResult fetchResult, bool isInitiatedByUser)
    {
        SignalInitialization();
        base.OnConfigFetched(fetchResult, isInitiatedByUser);
    }

    protected override void GoOnlineSynchronized()
    {
        // We need to restart the polling loop because going from offline to online should trigger a refresh operation
        // immediately instead of waiting for the next tick (which might not happen until much later).

        this.timerCancellationTokenSource.Cancel();
        this.timerCancellationTokenSource.Dispose();
        this.timerCancellationTokenSource = new CancellationTokenSource();

        StartScheduler(null, this.timerCancellationTokenSource.Token);
    }

    private void StartScheduler(Task<ProjectConfig>? initialCacheSyncUpTask, CancellationToken stopToken)
    {
        TaskShim.Current.Run(async () =>
        {
            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    var scheduledNextTime = DateTimeUtils.GetMonotonicTime() + this.pollInterval;
                    try
                    {
                        await PollCoreAsync(initialCacheSyncUpTask, stopToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        this.Logger.AutoPollConfigServiceErrorDuringPolling(ex);
                    }

                    var realNextTime = scheduledNextTime - DateTimeUtils.GetMonotonicTime();
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

                initialCacheSyncUpTask = null; // allow GC to collect the task and its result
            }

            return default(object);
        }, stopToken);
    }

    private async ValueTask PollCoreAsync(Task<ProjectConfig>? initialCacheSyncUpTask, CancellationToken stopToken)
    {
        var latestConfig = initialCacheSyncUpTask is not null
            ? await initialCacheSyncUpTask.WaitAsync(stopToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext)
            : await this.ConfigCache.GetAsync(base.CacheKey, stopToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

        if (!IsOffline && latestConfig.IsExpired(expiration: this.pollExpiration))
        {
            await RefreshConfigCoreAsync(latestConfig, isInitiatedByUser: false, stopToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            return; // postpone signalling initialization until OnConfigFetched
        }

        SignalInitialization();
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
