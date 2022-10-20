using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    [Obsolete("This class is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
    public class ConfigCatClientBuilder
    {
        internal string SdkKey { get; private set; }
        internal ILogger Logger { get; private set; } = new ConsoleLogger();
        internal DataGovernance DataGovernance { get; private set; } = DataGovernance.Global;

        /// <summary>
        /// Create a <see cref="ConfigCatClientBuilder"/> instance with <paramref name="sdkKey"/>
        /// </summary>
        /// <returns></returns>
        [Obsolete("To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
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
        [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.AutoPoll(); })' format.")]
        public AutoPollConfigurationBuilder WithAutoPoll()
        {
            return new AutoPollConfigurationBuilder(this);
        }

        /// <summary>
        /// Set ManualPoll mode
        /// </summary>
        [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.ManualPoll(); })' format.")]
        public ManualPollConfigurationBuilder WithManualPoll()
        {
            return new ManualPollConfigurationBuilder(this);
        }

        /// <summary>
        /// Set LazyLoad mode
        /// </summary>
        [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.LazyLoad(); })' format.")]
        public LazyLoadConfigurationBuilder WithLazyLoad()
        {
            return new LazyLoadConfigurationBuilder(this);
        }

        /// <summary>
        /// Set Logger instance
        /// </summary>
        /// <param name="logger">Implementation of <see cref="ILogger"/></param>
        /// <returns></returns>
        [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.Logger = /* your logger */; })' format.")]
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
        [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.DataGovernance = DataGovernance.Global; })' format.")]
        public ConfigCatClientBuilder WithDataGovernance(DataGovernance dataGovernance)
        {
            this.DataGovernance = dataGovernance;

            return this;
        }
    }
}