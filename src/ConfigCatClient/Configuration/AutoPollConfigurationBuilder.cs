using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration builder for AutoPoll mode
    /// </summary>
    public class AutoPollConfigurationBuilder : ConfigurationBuilderBase<AutoPollConfiguration>
    {
        internal AutoPollConfigurationBuilder(ConfigCatClientBuilder clientBuilder) : base(clientBuilder) { }

        /// <summary>
        /// Configuration refresh period
        /// </summary>
        public AutoPollConfigurationBuilder WithPollIntervalSeconds(uint pollIntervalSeconds)
        {
            this.configuration.PollIntervalSeconds = pollIntervalSeconds;

            return this;
        }

        /// <summary>
        /// Maximum waiting time between initialization and the first config acquisition in secconds. (Default value is 5.)
        /// </summary>
        public AutoPollConfigurationBuilder WithMaxInitWaitTimeSeconds(uint maxInitWaitTimeSeconds)
        {
            this.configuration.MaxInitWaitTimeSeconds = maxInitWaitTimeSeconds;

            return this;
        }

        /// <summary>
        /// If you want to use custom caching instead of the client's default InMemoryConfigCache, You can provide an implementation of IConfigCache.
        /// </summary>
        public AutoPollConfigurationBuilder WithConfigCache(IConfigCache configCache)
        {
            this.configuration.ConfigCache = configCache;

            return this;
        }

        /// <summary>
        /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat
        /// </summary>
        public AutoPollConfigurationBuilder WithBaseUrl(Uri baseUrl)
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