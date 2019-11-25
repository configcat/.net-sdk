using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class AutoPollConfigService : ConfigServiceBase, IConfigService
    {
        private readonly DateTime maxInitWaitExpire;

        private readonly Timer timer;

        public event OnConfigurationChangedEventHandler OnConfigurationChanged;

        internal AutoPollConfigService(
            IConfigFetcher configFetcher,
            IConfigCache configCache,
            TimeSpan pollingInterval,
            TimeSpan maxInitWaitTime,
            ILogger logger) : this(configFetcher, configCache, pollingInterval, maxInitWaitTime, logger, true)
        {
        }

        /// <summary>
        /// For test purpose only
        /// </summary>
        /// <param name="configFetcher"></param>
        /// <param name="configCache"></param>
        /// <param name="pollingInterval"></param>
        /// <param name="maxInitWaitTime"></param>
        /// <param name="logger"></param>
        /// <param name="startTimer"></param>
        internal AutoPollConfigService(
            IConfigFetcher configFetcher,
            IConfigCache configCache,
            TimeSpan pollingInterval,
            TimeSpan maxInitWaitTime,
            ILogger logger,
            bool startTimer) : base(configFetcher, configCache, logger)
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

        public Task<ProjectConfig> GetConfigAsync()
        {
            var d = this.maxInitWaitExpire - DateTime.UtcNow;

            if (d > TimeSpan.Zero)
            {
                Task.Run(() => RefreshLogic("init")).Wait(d);
            }

            return Task.FromResult(this.configCache.Get());
        }

        public async Task RefreshConfigAsync()
        {
            await RefreshLogicAsync("manual");
        }

        private async Task RefreshLogicAsync(object sender)
        {
            this.log.Debug($"RefreshLogic start [{sender}]");

            var latestConfig = this.configCache.Get();

            var newConfig = await this.configFetcher.Fetch(latestConfig);

            if (!latestConfig.Equals(newConfig))
            {
                this.log.Debug("config changed");

                this.configCache.Set(newConfig);

                OnConfigurationChanged?.Invoke(this, OnConfigurationChangedEventArgs.Empty);
            }
        }

        private void RefreshLogic(object sender)
        {
            this.RefreshLogicAsync(sender).Wait();
        }
    }
}