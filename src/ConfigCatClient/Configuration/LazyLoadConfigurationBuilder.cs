using System;
using System.Net.Http;

namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration builder for LazyLoad mode
    /// </summary>
    public class LazyLoadConfigurationBuilder : ConfigurationBuilderBase<LazyLoadConfiguration>
    {
        internal LazyLoadConfigurationBuilder(ConfigCatClientBuilder clientBuilder) : base(clientBuilder) { }

        /// <summary>
        /// Cache time to live value in seconds, minimum value is 1.
        /// </summary>
        /// <param name="cacheTimeToLiveSeconds"></param>        
        public LazyLoadConfigurationBuilder WithCacheTimeToLiveSeconds(uint cacheTimeToLiveSeconds)
        {
            this.configuration.CacheTimeToLiveSeconds = cacheTimeToLiveSeconds;

            return this;
        }

        /// <summary>
        /// If you want to use custom caching instead of the client's default InMemoryConfigCache, You can provide an implementation of IConfigCache.
        /// </summary>
        public LazyLoadConfigurationBuilder WithConfigCache(IConfigCache configCache)
        {
            this.configuration.ConfigCache = configCache;

            return this;
        }

        /// <summary>
        /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat
        /// </summary>
        public LazyLoadConfigurationBuilder WithBaseUrl(Uri baseUrl)
        {
            this.configuration.BaseUrl = baseUrl;

            return this;
        }

        /// <summary>
        /// HttpClientHandler to provide network credentials and proxy settings
        /// </summary>
        public LazyLoadConfigurationBuilder WithHttpClientHandler(HttpClientHandler httpClientHandler)
        {
            this.configuration.HttpClientHandler = httpClientHandler;

            return this;
        }

        /// <summary>
        /// Create a <see cref="IConfigCatClient"/> instance
        /// </summary>
        /// <returns></returns>
        public IConfigCatClient Create()
        {
            return new ConfigCatClient(this.configuration);
        }
    }    
}