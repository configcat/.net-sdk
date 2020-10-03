using System.Threading.Tasks;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class ManualPollConfigService : ConfigServiceBase, IConfigService
    {
        internal ManualPollConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, ILogger logger)
            : base(configFetcher, cacheParameters, logger) { }

        public async Task<ProjectConfig> GetConfigAsync()
        {
            return await this.configCache.GetAsync(base.cacheKey).ConfigureAwait(false);
        }

        public async Task RefreshConfigAsync()
        {
            var config = await this.configCache.GetAsync(base.cacheKey);

            config = await this.configFetcher.Fetch(config).ConfigureAwait(false);

            await this.configCache.SetAsync(base.cacheKey, config).ConfigureAwait(false);
        }
    }
}