using System;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class LazyLoadConfigService : ConfigServiceBase, IConfigService
    {
        private readonly TimeSpan cacheTimeToLive;        

        internal LazyLoadConfigService(IConfigFetcher configFetcher, IConfigCache configCache, ILoggerFactory loggerFactory, TimeSpan cacheTimeToLive)
            : base(configFetcher, configCache, loggerFactory.GetLogger(nameof(LazyLoadConfigService)))
        {   
            this.cacheTimeToLive = cacheTimeToLive;
        }

        public async Task<ProjectConfig> GetConfigAsync()
        {
            var config = this.configCache.Get();

            if (config.TimeStamp < DateTime.UtcNow.Add(-this.cacheTimeToLive))
            {
                this.log.Debug("config expired");

                return await RefreshConfigLogic(config).ConfigureAwait(false);
            }

            return config;
        }

        public async Task RefreshConfigAsync()
        {
            var config = this.configCache.Get();

            await RefreshConfigLogic(config).ConfigureAwait(false);
        }

        private async Task<ProjectConfig> RefreshConfigLogic(ProjectConfig config)
        {
            config = await this.configFetcher.Fetch(config).ConfigureAwait(false);

            this.configCache.Set(config);

            return config;
        }
    }
}