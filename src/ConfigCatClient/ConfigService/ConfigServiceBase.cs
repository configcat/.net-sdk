using System;

namespace ConfigCat.Client.ConfigService
{
    internal abstract class ConfigServiceBase : IDisposable
    {
        private bool disposedValue = false;

        protected readonly IConfigFetcher configFetcher;

        protected readonly IConfigCache configCache;

        protected readonly ILogger log;

        protected ConfigServiceBase(IConfigFetcher configFetcher, IConfigCache configCache, ILogger log)
        {
            this.configFetcher = configFetcher;

            this.configCache = configCache;

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