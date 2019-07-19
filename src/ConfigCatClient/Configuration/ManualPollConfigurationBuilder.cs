using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration builder for ManualPoll mode
    /// </summary>
    public class ManualPollConfigurationBuilder : ConfigurationBuilderBase<ManualPollConfiguration>
    {
        internal ManualPollConfigurationBuilder(ConfigCatClientBuilder clientBuilder) : base(clientBuilder) { }

        /// <summary>
        /// If you want to use custom caching instead of the client's default InMemoryConfigCache, You can provide an implementation of IConfigCache.
        /// </summary>
        public ManualPollConfigurationBuilder WithConfigCache(IConfigCache configCache)
        {
            this.configuration.ConfigCache = configCache;

            return this;
        }

        /// <summary>
        /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat
        /// </summary>
        public ManualPollConfigurationBuilder WithBaseUrl(Uri baseUrl)
        {
            this.configuration.BaseUrl = baseUrl;

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