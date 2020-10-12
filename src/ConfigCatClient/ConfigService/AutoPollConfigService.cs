using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class AutoPollConfigService : ConfigServiceBase, IConfigService
    {
        private readonly DateTime maxInitWaitExpire;

        private readonly Timer timer;

        private ManualResetEventSlim initializedEventSlim = new ManualResetEventSlim(false);

        public event OnConfigurationChangedEventHandler OnConfigurationChanged;

        internal AutoPollConfigService(
            IConfigFetcher configFetcher,
            CacheParameters cacheParameters,
            TimeSpan pollingInterval,
            TimeSpan maxInitWaitTime,
            ILogger logger) : this(configFetcher, cacheParameters, pollingInterval, maxInitWaitTime, logger, true)
        {
        }

        /// <summary>
        /// For test purpose only
        /// </summary>
        /// <param name="configFetcher"></param>
        /// <param name="cacheParameters"></param>
        /// <param name="pollingInterval"></param>
        /// <param name="maxInitWaitTime"></param>
        /// <param name="logger"></param>
        /// <param name="startTimer"></param>
        internal AutoPollConfigService(
            IConfigFetcher configFetcher,
            CacheParameters cacheParameters,
            TimeSpan pollingInterval,
            TimeSpan maxInitWaitTime,
            ILogger logger,
            bool startTimer
            ) : base(configFetcher, cacheParameters, logger)
        {
            if (startTimer)
            {
                this.timer = new Timer(RefreshLogic, "auto", TimeSpan.Zero, pollingInterval);
            }

            this.maxInitWaitExpire = DateTime.UtcNow.Add(maxInitWaitTime);
        }

        protected override void Dispose(bool disposing)
        {
            this.timer?.Dispose();

            base.Dispose(disposing);
        }

        public async Task<ProjectConfig> GetConfigAsync()
        {
            var delay = this.maxInitWaitExpire - DateTime.UtcNow;

            var cacheConfig = await this.configCache.GetAsync(base.cacheKey).ConfigureAwait(false);

            if (delay > TimeSpan.Zero && cacheConfig.Equals(ProjectConfig.Empty))
            {
                if (!initializedEventSlim.Wait(delay))
                {
                    await RefreshLogicAsync("init");
                }

                cacheConfig = await this.configCache.GetAsync(base.cacheKey).ConfigureAwait(false);
            }

            return cacheConfig;
        }

        public async Task RefreshConfigAsync()
        {
            await RefreshLogicAsync("manual");
        }

        private async Task RefreshLogicAsync(object sender)
        {
            this.log.Debug($"RefreshLogic start [{sender}]");

            var latestConfig = await this.configCache.GetAsync(base.cacheKey).ConfigureAwait(false);

            var newConfig = await this.configFetcher.Fetch(latestConfig).ConfigureAwait(false);

            if (!latestConfig.Equals(newConfig) && !newConfig.Equals(ProjectConfig.Empty))
            {
                this.log.Debug("config changed");

                await this.configCache.SetAsync(base.cacheKey, newConfig).ConfigureAwait(false);

                OnConfigurationChanged?.Invoke(this, OnConfigurationChangedEventArgs.Empty);

                initializedEventSlim.Set();
            }
        }

        private void RefreshLogic(object sender)
        {
            this.RefreshLogicAsync(sender).Wait();
        }
    }
}