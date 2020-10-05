namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    public class ConfigCatClientBuilder
    {
        internal string SdkKey { get; private set; }
        internal ILogger Logger { get; private set; } = new ConsoleLogger();
        internal DataGovernance DataGovernance { get; private set; } = DataGovernance.Global;

        /// <summary>
        /// Create a <see cref="ConfigCatClientBuilder"/> instance with <paramref name="sdkKey"/>
        /// </summary>
        /// <returns></returns>
        public static ConfigCatClientBuilder Initialize(string sdkKey)
        {
            return new ConfigCatClientBuilder
            {
                SdkKey = sdkKey
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

        /// <summary>
        /// Set Logger instance
        /// </summary>
        /// <param name="logger">Implementation of <see cref="ILogger"/></param>
        /// <returns></returns>
        public ConfigCatClientBuilder WithLogger(ILogger logger)
        {
            this.Logger = logger;

            return this;
        }

        /// <summary>
        /// Set <see cref="DataGovernance" />
        /// </summary>
        /// <param name="dataGovernance">Describes the location of your feature flag and setting data within the ConfigCat CDN.</param>
        /// <returns></returns>
        public ConfigCatClientBuilder WithDataGovernance(DataGovernance dataGovernance)
        {
            this.DataGovernance = dataGovernance;

            return this;
        }
    }
}