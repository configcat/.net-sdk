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
        private readonly CancellationTokenSource timerCancellationTokenSource = new();

        internal AutoPollConfigService(
            AutoPoll configuration,
            IConfigFetcher configFetcher,
            CacheParameters cacheParameters,
            LoggerWrapper logger) : this(configuration, configFetcher, cacheParameters, logger, true)
        { }

        // For test purposes only
        internal AutoPollConfigService(
            AutoPoll configuration,
            IConfigFetcher configFetcher,
            CacheParameters cacheParameters,
            LoggerWrapper logger,
            bool startTimer
            ) : base(configFetcher, cacheParameters, logger)
        {
            this.configuration = configuration;
            this.maxInitWaitExpire = DateTimeOffset.UtcNow.Add(configuration.MaxInitWaitTime);

            if (startTimer)
            {
                StartScheduler(configuration.PollInterval);
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.timerCancellationTokenSource.Cancel();
            this.initializedEventSlim.Dispose();
            base.Dispose(disposing);
        }


        public ProjectConfig GetConfig()
        {
            if (this.ConfigCache is IConfigCatCache cache)
            {
                var delay = this.maxInitWaitExpire.Subtract(DateTimeOffset.UtcNow);
                var cacheConfig = cache.Get(base.CacheKey);

                if (delay > TimeSpan.Zero && cacheConfig.Equals(ProjectConfig.Empty))
                {
                    initializedEventSlim.Wait(delay);
                    cacheConfig = cache.Get(base.CacheKey);
                }

                return cacheConfig;
            }

            // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
            return Syncer.Sync(this.GetConfigAsync);
        }

        public async Task<ProjectConfig> GetConfigAsync()
        {
            var delay = this.maxInitWaitExpire.Subtract(DateTimeOffset.UtcNow);
            var cacheConfig = await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);

            if (delay > TimeSpan.Zero && cacheConfig.Equals(ProjectConfig.Empty))
            {
                initializedEventSlim.Wait(delay);
                cacheConfig = await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);
            }

            return cacheConfig;
        }

        public void RefreshConfig()
        {
            // check for the new cache interface until we remove the old IConfigCache.
            if (this.ConfigCache is IConfigCatCache cache)
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

                return;
            }

            // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
            Syncer.Sync(this.RefreshConfigAsync);
        }

        public async Task RefreshConfigAsync()
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

        private void StartScheduler(TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (!this.timerCancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var scheduledNextTime = DateTimeOffset.UtcNow.Add(interval);
                        try
                        {
                            await RefreshConfigAsync();
                        }
                        catch (Exception exception)
                        {
                            this.Log.Error($"Error occured during polling. {exception.Message}");
                        }
                        finally
                        {
                            var realNextTime = scheduledNextTime.Subtract(DateTimeOffset.UtcNow);
                            if (realNextTime > TimeSpan.Zero)
                            {
                                await Task.Delay(realNextTime, this.timerCancellationTokenSource.Token);
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