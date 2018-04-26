using ConfigCat.Client.Configuration;

namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    public class ConfigCatClientBuilder
    {
        internal string ApiKey { get; private set; }

        /// <summary>
        /// Create a <see cref="ConfigCatClientBuilder"/> instance with <paramref name="apiKey"/>
        /// </summary>
        /// <returns></returns>
        public static ConfigCatClientBuilder Initialize(string apiKey)
        {
            return new ConfigCatClientBuilder
            {
                ApiKey = apiKey
            };
        }

        /// <summary>
        /// Set AutoPoll mode
        /// </summary>        
        public AutoPollConfigurationBuilder WithAutoPoll()
        {
            return new AutoPollConfigurationBuilder(this);
        }

        /// <summary>
        /// Set ManualPoll mode
        /// </summary>
        public ManualPollConfigurationBuilder WithManualPoll()
        {
            return new ManualPollConfigurationBuilder(this);
        }

        /// <summary>
        /// Set LazyLoad mode
        /// </summary>
        public LazyLoadConfigurationBuilder WithLazyLoad()
        {
            return new LazyLoadConfigurationBuilder(this);
        }
    }
}