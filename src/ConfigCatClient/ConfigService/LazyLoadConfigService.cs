using System;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class LazyLoadConfigService : ConfigServiceBase, IConfigService
    {
        private readonly TimeSpan cacheTimeToLive;
        
        internal LazyLoadConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger, TimeSpan cacheTimeToLive, bool isOffline = false, Hooks hooks = null)
            : base(configFetcher, cacheParameters, logger, isOffline, hooks)
        {
            this.cacheTimeToLive = cacheTimeToLive;

            hooks?.RaiseClientReady();
        }

        public ProjectConfig GetConfig()
        {
            // check for the new cache interface until we remove the old IConfigCache.
            if (this.ConfigCache is IConfigCatCache cache)
            {
                var config = cache.Get(base.CacheKey);

                if (config.IsExpired(expiration: this.cacheTimeToLive, out var cachedConfigIsEmpty))
                {
                    if (!cachedConfigIsEmpty)
                    {
                        OnConfigExpired();
                    }

                    if (!IsOffline)
                    {
                        var configWithFetchResult = RefreshConfigCore(config);
                        return configWithFetchResult.Item1;
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

            if (config.IsExpired(expiration: this.cacheTimeToLive, out var cachedConfigIsEmpty))
            {
                if (!cachedConfigIsEmpty)
                {
                    OnConfigExpired();
                }

                if (!IsOffline)
                {
                    var configWithFetchResult = await RefreshConfigCoreAsync(config).ConfigureAwait(false);
                    return configWithFetchResult.Item1;
                }
            }

            return config;
        }

        private void OnConfigExpired()
        {
            this.Log.Debug("config expired");
        }
    }
}