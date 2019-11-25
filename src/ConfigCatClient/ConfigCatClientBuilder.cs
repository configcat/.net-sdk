namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    public class ConfigCatClientBuilder
    {
        internal string ApiKey { get; private set; }
        internal ILogger Logger { get; private set; } = new ConsoleLogger();

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

        /// <summary>
        /// Set Logger instance
        /// </summary>
        /// <param name="logger">Implementation of <c>ILogger</c></param>
        /// <returns></returns>
        public ConfigCatClientBuilder WithLogger(ILogger logger)
        {
            this.Logger = logger;

            return this;
        }
    }
}