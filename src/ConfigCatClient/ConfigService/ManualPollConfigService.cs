﻿using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class ManualPollConfigService : ConfigServiceBase, IConfigService
    {
        internal ManualPollConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline = false)
            : base(configFetcher, cacheParameters, logger, isOffline) { }

        public ProjectConfig GetConfig()
        {
            // check for the new cache interface until we remove the old IConfigCache.
            if (this.ConfigCache is IConfigCatCache cache)
            {
                return cache.Get(base.CacheKey);
            }

            // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
            return Syncer.Sync(this.GetConfigAsync);
        }

        public async Task<ProjectConfig> GetConfigAsync()
        {
            return await this.ConfigCache.GetAsync(base.CacheKey).ConfigureAwait(false);
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
                config = await this.ConfigFetcher.FetchAsync(config).ConfigureAwait(false);
                await this.ConfigCache.SetAsync(base.CacheKey, config).ConfigureAwait(false);
            }
            else
            {
                this.Log.OfflineModeWarning();
            }
        }
    }
}