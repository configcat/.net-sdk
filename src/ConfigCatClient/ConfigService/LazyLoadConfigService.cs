using System;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class LazyLoadConfigService : ConfigServiceBase, IConfigService
    {
        private readonly TimeSpan cacheTimeToLive;        

        internal LazyLoadConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, ILogger logger, TimeSpan cacheTimeToLive)
            : base(configFetcher, cacheParameters, logger)
        {   
            this.cacheTimeToLive = cacheTimeToLive;
        }

        public async Task<ProjectConfig> GetConfigAsync()
        {
            var config = await this.configCache.GetAsync(base.cacheKey).ConfigureAwait(false);

            if (config.TimeStamp < DateTime.UtcNow.Add(-this.cacheTimeToLive))
            {
                this.log.Debug("config expired");

                return await RefreshConfigLogic(config).ConfigureAwait(false);
            }

            return config;
        }

        public async Task RefreshConfigAsync()
        {
            var config = await this.configCache.GetAsync(base.cacheKey).ConfigureAwait(false);

            await RefreshConfigLogic(config).ConfigureAwait(false);
        }

        private async Task<ProjectConfig> RefreshConfigLogic(ProjectConfig config)
        {
            config = await this.configFetcher.Fetch(config).ConfigureAwait(false);

            await this.configCache.SetAsync(base.cacheKey, config).ConfigureAwait(false);

            return config;
        }
    }
}