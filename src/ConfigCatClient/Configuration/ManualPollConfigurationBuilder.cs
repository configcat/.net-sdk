using System;
using System.Net.Http;

namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration builder for ManualPoll mode
    /// </summary>
    [Obsolete("Please use the 'new ConfigCatClient(options => { options.PollingMode = PollingModes.ManualPoll(); })' format.")]
    public class ManualPollConfigurationBuilder : ConfigurationBuilderBase<ManualPollConfiguration>
    {
        internal ManualPollConfigurationBuilder(ConfigCatClientBuilder clientBuilder) : base(clientBuilder) { }

        /// <summary>
        /// If you want to use custom caching instead of the client's default InMemoryConfigCache, You can provide an implementation of IConfigCache.
        /// </summary>
        [Obsolete("Please use the 'new ConfigCatClient(options => { options.ConfigCache = /* your cache */; })' format.")]
        public ManualPollConfigurationBuilder WithConfigCache(IConfigCache configCache)
        {
            this.configuration.ConfigCache = configCache;

            return this;
        }

        /// <summary>
        /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat
        /// </summary>
        [Obsolete("Please use the 'new ConfigCatClient(options => { options.BaseUrl = new Uri(/* base url */); })' format.")]
        public ManualPollConfigurationBuilder WithBaseUrl(Uri baseUrl)
        {
            this.configuration.BaseUrl = baseUrl;

            return this;
        }

        /// <summary>
        /// HttpClientHandler to provide network credentials and proxy settings
        /// </summary>
        [Obsolete("Please use the 'new ConfigCatClient(options => { options.HttpClientHandler = /* http client handler */; })' format.")]
        public ManualPollConfigurationBuilder WithHttpClientHandler(HttpClientHandler httpClientHandler)
        {
            this.configuration.HttpClientHandler = httpClientHandler;

            return this;
        }

        /// <summary>
        /// Create a <see cref="IConfigCatClient"/> instance
        /// </summary>
        /// <returns></returns>
        [Obsolete("Please use the 'new ConfigCatClient(options => { options.PollingMode = PollingModes.ManualPoll; })' format.")]
        public IConfigCatClient Create()
        {
            return new ConfigCatClient(this.configuration);
        }
    }
}