namespace ConfigCat.Client.Configuration
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
        /// Create a <see cref="IConfigCatClient"/> instance
        /// </summary>
        /// <returns></returns>
        public IConfigCatClient Create()
        {
            return new ConfigCatClient(this.configuration);
        }
    }
}