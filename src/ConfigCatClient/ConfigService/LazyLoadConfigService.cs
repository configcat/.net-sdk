using System;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class LazyLoadConfigService : ConfigServiceBase, IConfigService
    {
        private readonly TimeSpan cacheTimeToLive;
        
        internal LazyLoadConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, TimeSpan cacheTimeToLive, bool isOffline = false)
            : base(configFetcher, cacheParameters, logger, isOffline)
        {
            this.cacheTimeToLive = cacheTimeToLive;
        }

        public ProjectConfig GetConfig()
        {
            // check for the new cache interface until we remove the old IConfigCache.
            if (this.ConfigCache is IConfigCatCache cache)
            {
                var config = cache.Get(base.CacheKey);

                if (config.TimeStamp < DateTime.UtcNow.Subtract(this.cacheTimeToLive))
                {
                    this.Log.Debug("config expired");
                    if (!IsOffline)
                    {
                        config = this.ConfigFetcher.Fetch(config);
                        cache.Set(base.CacheKey, config);
                    }
                }

                return config;
            }

            // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
            return Syncer.Sync(this.GetConfigAsync);
        }

        public async Task<ProjectConfig> GetConfigAsync()
        {
            var config = await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);

            if (config.TimeStamp < DateTime.UtcNow.Subtract(this.cacheTimeToLive))
            {
                this.Log.Debug("config expired");
                if (!IsOffline)
                {
                    return await RefreshConfigLogic(config).ConfigureAwait(false);
                }
            }

            return config;
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
                    cache.Set(base.CacheKey, newConfig);
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

        public async Task RefreshConfigAsync()
        {
            if (!IsOffline)
            {
                var config = await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);
                await RefreshConfigLogic(config).ConfigureAwait(false);
            }
            else
            {
                this.Log.OfflineModeWarning();
            }
        }

        private async Task<ProjectConfig> RefreshConfigLogic(ProjectConfig config)
        {
            config = await this.ConfigFetcher.FetchAsync(config).ConfigureAwait(false);
            await this.ConfigCache.SetAsync(base.CacheKey, config).ConfigureAwait(false);
            return config;
        }
    }
}