using System;
using System.Net.Http;

namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration builder for ManualPoll mode
    /// </summary>
    [Obsolete("This class is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance with manual polling for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.ManualPoll(); })' format.")]
    public class ManualPollConfigurationBuilder : ConfigurationBuilderBase<ManualPollConfiguration>
    {
        internal ManualPollConfigurationBuilder(ConfigCatClientBuilder clientBuilder) : base(clientBuilder) { }

        /// <summary>
        /// If you want to use custom caching instead of the client's default InMemoryConfigCache, You can provide an implementation of IConfigCache.
        /// </summary>
        [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.ConfigCache = /* your cache */; })' format.")]
        public ManualPollConfigurationBuilder WithConfigCache(IConfigCache configCache)
        {
            this.configuration.ConfigCache = configCache;

            return this;
        }

        /// <summary>
        /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat
        /// </summary>
        [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.BaseUrl = new Uri(/* base url */); })' format.")]
        public ManualPollConfigurationBuilder WithBaseUrl(Uri baseUrl)
        {
            this.configuration.BaseUrl = baseUrl;

            return this;
        }

        /// <summary>
        /// HttpClientHandler to provide network credentials and proxy settings
        /// </summary>
        [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.HttpClientHandler = /* http client handler */; })' format.")]
        public ManualPollConfigurationBuilder WithHttpClientHandler(HttpClientHandler httpClientHandler)
        {
            this.configuration.HttpClientHandler = httpClientHandler;

            return this;
        }

        /// <summary>
        /// Create a <see cref="IConfigCatClient"/> instance
        /// </summary>
        /// <returns></returns>
        [Obsolete("To obtain a ConfigCatClient instance with manual polling for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.ManualPoll(); })' format.")]
        public IConfigCatClient Create()
        {
            return new ConfigCatClient(this.configuration);
        }
    }
}