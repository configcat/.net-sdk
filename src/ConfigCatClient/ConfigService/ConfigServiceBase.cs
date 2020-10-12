using System;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.ConfigService
{
    internal abstract class ConfigServiceBase : IDisposable
    {
        private bool disposedValue = false;

        protected readonly IConfigFetcher configFetcher;

        protected readonly IConfigCache configCache;

        protected readonly ILogger log;

        protected readonly string cacheKey;

        protected ConfigServiceBase(IConfigFetcher configFetcher, CacheParameters cacheParameters, ILogger log)
        {
            this.configFetcher = configFetcher;

            this.configCache = cacheParameters.ConfigCache;

            this.cacheKey = cacheParameters.CacheKey;

            this.log = log;
        }       

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (configFetcher != null && configFetcher is IDisposable)
                    {
                        ((IDisposable)configFetcher).Dispose();
                    }
                }

                disposedValue = true;
            }
        }
                
        public void Dispose()
        {
            Dispose(true);         
        }
    }
}