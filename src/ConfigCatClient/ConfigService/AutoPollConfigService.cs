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
    private readonly TaskCompletionSource<object?> initSignalTcs; // used for signalling initialization ready
    private readonly Task<object?> initSignalTask;
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

        this.initSignalTcs = TaskShim.CreateSafeCompletionSource(out this.initSignalTask);

        PrepareClientForEvents(this, hooks);

        var initialCacheSyncUpTask = SyncUpWithCacheAsync().AsTask();

        // This task will complete as soon as
        // 1. a cache sync operation completes, and the obtained config is up-to-date (see GetConfigAsync and PollCoreAsync),
        // 2. or, in case the client is online and the internal cache is still empty or expired after the initial cache sync-up,
        //    the first config fetch operation completes, regardless of success or failure (see OnConfigFetched).
        // If the service gets disposed before any of these events happen, the task will also complete, but with a canceled status.
        InitializationTask = WaitForInitializationAsync(DisposeToken);

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

    private bool IsInitialized => InitializationTask.Status == TaskStatus.RanToCompletion;

    private void SignalInitialization()
    {
        this.initSignalTcs.TrySetResult(null);
    }

    private async Task<bool> WaitForInitializationAsync(CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        try
        {
            await this.initSignalTask.WaitAsync(this.maxInitWaitTime, cancellationToken)
                .ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
#else
        using var timerCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timerCts.Token, cancellationToken);
        var completedTask = await Task.WhenAny(this.initSignalTask, TaskShim.Current.Delay(this.maxInitWaitTime, linkedCts.Token))
            .ConfigureAwait(TaskShim.ContinueOnCapturedContext);

        if (ReferenceEquals(completedTask, this.initSignalTask))
        {
            timerCts.Cancel(); // make sure that the underlying timer of the Delay task is released
            return true;
        }
        else if (completedTask.IsCanceled)
        {
            completedTask.GetAwaiter().GetResult(); // propagate cancellation
        }
        return false;
#endif
    }

    protected override async ValueTask<ClientCacheState> WaitForReadyAsync(Task<ProjectConfig> initialCacheSyncUpTask)
    {
        await InitializationTask.ConfigureAwait(TaskShim.ContinueOnCapturedContext);
        return GetCacheState(this.ConfigCache.LocalCachedConfig);
    }

    public async ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var cachedConfig = await SyncUpWithCacheAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

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
            : await SyncUpWithCacheAsync(stopToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

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
