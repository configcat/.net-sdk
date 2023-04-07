using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client.ConfigService;

internal sealed class AutoPollConfigService : ConfigServiceBase, IConfigService
{
    private readonly AutoPoll configuration;
    private readonly CancellationTokenSource initializationCancellationTokenSource = new(); // used for signalling initialization
    private CancellationTokenSource? timerCancellationTokenSource = new(); // used for signalling background work to stop

    internal AutoPollConfigService(
        AutoPoll configuration,
        IConfigFetcher configFetcher,
        CacheParameters cacheParameters,
        LoggerWrapper logger,
        bool isOffline = false,
        Hooks? hooks = null) : this(configuration, configFetcher, cacheParameters, logger, startTimer: true, isOffline, hooks)
    { }

    // For test purposes only
    internal AutoPollConfigService(
        AutoPoll configuration,
        IConfigFetcher configFetcher,
        CacheParameters cacheParameters,
        LoggerWrapper logger,
        bool startTimer,
        bool isOffline = false,
        Hooks? hooks = null) : base(configFetcher, cacheParameters, logger, isOffline, hooks)
    {
        this.configuration = configuration;

        this.initializationCancellationTokenSource.Token.Register(this.Hooks.RaiseClientReady, useSynchronizationContext: false);
        if (configuration.MaxInitWaitTime > TimeSpan.Zero)
        {
            this.initializationCancellationTokenSource.CancelAfter(configuration.MaxInitWaitTime);
        }
        else
        {
            this.initializationCancellationTokenSource.Cancel();
        }

        if (!isOffline && startTimer)
        {
            StartScheduler(configuration.PollInterval);
        }
    }

    protected override void DisposeSynchronized(bool disposing)
    {
        // Background work should stop under all circumstances
        this.timerCancellationTokenSource!.Cancel();

        if (disposing)
        {
            this.timerCancellationTokenSource.Dispose();
            this.timerCancellationTokenSource = null;
        }

        base.DisposeSynchronized(disposing);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.initializationCancellationTokenSource.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool IsInitialized => this.initializationCancellationTokenSource.IsCancellationRequested;

    private void SignalInitialization()
    {
        try
        {
            this.initializationCancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Since SignalInitialization and Dispose are not synchronized,
            // in extreme conditions a call to SignalInitialization may slip past the disposal of initializationCancellationTokenSource.
            // In such cases we get an ObjectDisposedException here, which means that the config service has been disposed of in the meantime.
            // Thus, we can safely swallow this exception.
        }
    }

    internal bool WaitForInitialization()
    {
        // An infinite timeout would also work but we limit waiting to MaxInitWaitTime for maximum safety.
        return this.initializationCancellationTokenSource.Token.WaitHandle.WaitOne(this.configuration.MaxInitWaitTime);
    }

    internal async Task<bool> WaitForInitializationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // An infinite timeout would also work but we limit waiting to MaxInitWaitTime for maximum safety.
            await Task.Delay(this.configuration.MaxInitWaitTime, this.initializationCancellationTokenSource.Token)
                .WaitAsync(cancellationToken).ConfigureAwait(false);

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
            var cacheConfig = this.ConfigCache.Get(base.CacheKey);
            if (!cacheConfig.IsExpired(expiration: this.configuration.PollInterval, out _))
            {
                return cacheConfig;
            }

            WaitForInitialization();
        }

        return this.ConfigCache.Get(base.CacheKey);
    }

    public async Task<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        if (!IsOffline && !IsInitialized)
        {
            var cacheConfig = await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(false);
            if (!cacheConfig.IsExpired(expiration: this.configuration.PollInterval, out _))
            {
                return cacheConfig;
            }

            await WaitForInitializationAsync(cancellationToken).ConfigureAwait(false);
        }

        return await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(false);
    }

    protected override void OnConfigUpdated(ProjectConfig newConfig)
    {
        base.OnConfigUpdated(newConfig);
        SignalInitialization();
    }

    protected override void SetOnlineCoreSynchronized()
    {
        StartScheduler(this.configuration.PollInterval);
    }

    protected override void SetOfflineCoreSynchronized()
    {
        this.timerCancellationTokenSource!.Cancel();
        this.timerCancellationTokenSource.Dispose();
        this.timerCancellationTokenSource = new CancellationTokenSource();
    }

    private void StartScheduler(TimeSpan interval)
    {
        Task.Run(async () =>
        {
            var isFirstIteration = true;

            while (Synchronize(static @this => @this.timerCancellationTokenSource?.Token, this) is { } cancellationToken
                && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var scheduledNextTime = DateTime.UtcNow.Add(interval);
                    try
                    {
                        await PollCoreAsync(isFirstIteration, interval, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        this.Logger.AutoPollConfigServiceErrorDuringPolling(ex);
                    }

                    var realNextTime = scheduledNextTime.Subtract(DateTime.UtcNow);
                    if (realNextTime > TimeSpan.Zero)
                    {
                        await Task.Delay(realNextTime, cancellationToken).ConfigureAwait(false);
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
            }
        });
    }

    private async Task PollCoreAsync(bool isFirstIteration, TimeSpan interval, CancellationToken cancellationToken)
    {
        if (isFirstIteration)
        {
            var latestConfig = await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(false);
            if (latestConfig.IsExpired(expiration: interval, out _))
            {
                if (!IsOffline)
                {
                    await RefreshConfigCoreAsync(latestConfig, cancellationToken).ConfigureAwait(false);
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
                var latestConfig = await this.ConfigCache.GetAsync(base.CacheKey, cancellationToken).ConfigureAwait(false);
                await RefreshConfigCoreAsync(latestConfig, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    internal void StopScheduler()
    {
        Synchronize(static @this =>
        {
            @this.timerCancellationTokenSource?.Cancel();
            return default(object);
        }, this);
    }
}
