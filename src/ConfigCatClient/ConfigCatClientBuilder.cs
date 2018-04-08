using ConfigCat.Client.Configuration;

namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    public class ConfigCatClientBuilder
    {
        internal string ProjectSecret { get; private set; }

        /// <summary>
        /// Create a <see cref="ConfigCatClientBuilder"/> instance with <paramref name="projectSecret"/>
        /// </summary>
        /// <returns></returns>
        public static ConfigCatClientBuilder Initialize(string projectSecret)
        {
            return new ConfigCatClientBuilder
            {
                ProjectSecret = projectSecret
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