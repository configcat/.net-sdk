using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class AutoPollConfigService : ConfigServiceBase, IConfigService
    {
        private readonly DateTimeOffset maxInitWaitExpire;
        private readonly ManualResetEventSlim initializedEventSlim = new(false);
        private readonly AutoPoll configuration;
        private readonly WeakReference<IConfigCatClient> clientWeakRef;
        private CancellationTokenSource timerCancellationTokenSource = new();

        internal AutoPollConfigService(
            AutoPoll configuration,
            IConfigFetcher configFetcher,
            CacheParameters cacheParameters,
            LoggerWrapper logger,
            bool isOffline = false,
            WeakReference<IConfigCatClient> clientWeakRef = null) : this(configuration, configFetcher, cacheParameters, logger, startTimer: true, isOffline, clientWeakRef)
        { }

        // For test purposes only
        internal AutoPollConfigService(
            AutoPoll configuration,
            IConfigFetcher configFetcher,
            CacheParameters cacheParameters,
            LoggerWrapper logger,
            bool startTimer,
            bool isOffline = false,
            WeakReference<IConfigCatClient> clientWeakRef = null) : base(configFetcher, cacheParameters, logger, isOffline)
        {
            this.configuration = configuration;
            this.maxInitWaitExpire = DateTimeOffset.UtcNow.Add(configuration.MaxInitWaitTime);
            this.clientWeakRef = clientWeakRef;

            if (!isOffline && startTimer)
            {
                StartScheduler(configuration.PollInterval);
            }
        }

        protected override void DisposeSynchronized(bool disposing)
        {
            // Background work should stop under all circumstances
            this.timerCancellationTokenSource.Cancel();

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
                this.initializedEventSlim.Dispose();
            }

            base.Dispose(disposing);
        }

        internal bool WaitForInitialization(TimeSpan timeout)
        {
            return this.initializedEventSlim.Wait(timeout);
        }

        public ProjectConfig GetConfig()
        {
            if (this.ConfigCache is IConfigCatCache cache)
            {
                var cacheConfig = cache.Get(base.CacheKey);

                if (!IsOffline)
                {
                    var delay = this.maxInitWaitExpire.Subtract(DateTimeOffset.UtcNow);
                    if (delay > TimeSpan.Zero && cacheConfig.Equals(ProjectConfig.Empty))
                    {
                        WaitForInitialization(delay);
                        cacheConfig = cache.Get(base.CacheKey);
                    }
                }

                return cacheConfig;
            }

            // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
            return Syncer.Sync(this.GetConfigAsync);
        }

        public async Task<ProjectConfig> GetConfigAsync()
        {
            var cacheConfig = await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);

            if (!IsOffline)
            {
                var delay = this.maxInitWaitExpire.Subtract(DateTimeOffset.UtcNow);
                if (delay > TimeSpan.Zero && cacheConfig.Equals(ProjectConfig.Empty))
                {
                    WaitForInitialization(delay);
                    cacheConfig = await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);
                }
            }

            return cacheConfig;
        }

        public void RefreshConfig()
        {
            // check for the new cache interface until we remove the old IConfigCache.
            if (this.ConfigCache is IConfigCatCache cache)
            {
                if (!IsOffline)
                {
                    var latestConfig = cache.Get(base.CacheKey);
                    var newConfig = this.ConfigFetcher.Fetch(latestConfig);

                    if (!latestConfig.Equals(newConfig) && !newConfig.Equals(ProjectConfig.Empty))
                    {
                        this.Log.Debug("config changed");
                        cache.Set(base.CacheKey, newConfig);
                        this.configuration.RaiseOnConfigurationChanged(this, OnConfigurationChangedEventArgs.Empty);
                        initializedEventSlim.Set();
                    }
                }
                else
                {
                    this.Log.OfflineModeWarning();
                }

                return;
            }

            // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
            Syncer.Sync(this.RefreshConfigAsync);
        }

        private async Task RefreshConfigCoreAsync()
        {
            var latestConfig = await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);
            var newConfig = await this.ConfigFetcher.FetchAsync(latestConfig).ConfigureAwait(false);

            if (!latestConfig.Equals(newConfig) && !newConfig.Equals(ProjectConfig.Empty))
            {
                this.Log.Debug("config changed");
                await this.ConfigCache.SetAsync(base.CacheKey, newConfig).ConfigureAwait(false);
                this.configuration.RaiseOnConfigurationChanged(this, OnConfigurationChangedEventArgs.Empty);
                initializedEventSlim.Set();
            }
        }

        public async Task RefreshConfigAsync()
        {
            if (!IsOffline)
            {
                await RefreshConfigCoreAsync().ConfigureAwait(false);
            }
            else
            {
                this.Log.OfflineModeWarning();
            }
        }

        protected override void SetOnlineCoreSynchronized()
        {
            StartScheduler(configuration.PollInterval);
        }

        protected override void SetOfflineCoreSynchronized()
        {
            this.timerCancellationTokenSource.Cancel();
            this.timerCancellationTokenSource.Dispose();
            this.timerCancellationTokenSource = new CancellationTokenSource();
        }

        private void StartScheduler(TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while ((this.clientWeakRef is null || this.clientWeakRef.IsAlive()) && Synchronize(static @this => @this.timerCancellationTokenSource, this) is CancellationTokenSource cts && !cts.IsCancellationRequested)
                {
                    try
                    {
                        var scheduledNextTime = DateTimeOffset.UtcNow.Add(interval);
                        try
                        {
                            if (!IsOffline)
                            {
                                await RefreshConfigCoreAsync().ConfigureAwait(false);
                            }
                        }
                        catch (Exception exception)
                        {
                            this.Log.Error($"Error occured during polling. {exception.Message}");
                        }
                        finally
                        {
                            if (this.clientWeakRef is null || this.clientWeakRef.IsAlive())
                            {
                                var realNextTime = scheduledNextTime.Subtract(DateTimeOffset.UtcNow);
                                if (realNextTime > TimeSpan.Zero)
                                {
                                    await Task.Delay(realNextTime, cts.Token).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore exceptions from cancellation.
                    }
                    catch (Exception exception)
                    {
                        this.Log.Error($"Error occured during polling. {exception.Message}");
                    }
                }
            });
        }
    }
}