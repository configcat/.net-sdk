using System;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.ConfigService
{
    internal abstract class ConfigServiceBase : IDisposable
    {
        private bool disposedValue;

        protected readonly IConfigFetcher ConfigFetcher;

#pragma warning disable CS0618 // Type or member is obsolete
        protected readonly IConfigCache ConfigCache; // Backward compatibility, it'll be changed to IConfigCatCache later.
#pragma warning restore CS0618 // Type or member is obsolete

        protected readonly ILogger Log;

        protected readonly string CacheKey;

        protected ConfigServiceBase(IConfigFetcher configFetcher, CacheParameters cacheParameters, ILogger log)
        {
            this.ConfigFetcher = configFetcher;
            this.ConfigCache = cacheParameters.ConfigCache;
            this.CacheKey = cacheParameters.CacheKey;
            this.Log = log;
        }       

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing && ConfigFetcher is IDisposable disposable)
            {
                disposable.Dispose();
            }

            disposedValue = true;
        }
                
        public void Dispose()
        {
            Dispose(true);         
        }
    }
}