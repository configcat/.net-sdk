using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Logging;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class AutoPollConfigService : ConfigServiceBase, IConfigService, IDisposable
    {
        private readonly DateTime maxInitWaitExpire;

        private readonly Timer timer;

        public event OnConfigurationChangedEventHandler OnConfigurationChanged;

        internal AutoPollConfigService(
            IConfigFetcher configFetcher,
            IConfigCache configCache,
            TimeSpan pollingInterval,
            TimeSpan maxInitWaitTime,
            ILoggerFactory loggerFactory) : base(configFetcher, configCache, loggerFactory.GetLogger(nameof(AutoPollConfigService)))
        {
            this.timer = new Timer(RefreshLogic, "auto", TimeSpan.Zero, pollingInterval);

            this.maxInitWaitExpire = DateTime.UtcNow.Add(maxInitWaitTime);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.timer?.Dispose();
        }

        public Task<ProjectConfig> GetConfigAsync()
        {
            var d = this.maxInitWaitExpire - DateTime.UtcNow;

            if (d > TimeSpan.Zero)
            {
                Task.Run(() => RefreshLogic(null)).Wait(d);
            }

            return Task.FromResult(this.configCache.Get());
        }

        public async Task RefreshConfigAsync()
        {
            await RefreshLogicAsync("manual");
        }

        private async Task RefreshLogicAsync(object sender)
        {
            this.log.Debug($"RefreshLogic '{0}' start");

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