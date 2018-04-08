namespace ConfigCat.Client.Configuration
{
    /// <summary>
    /// Configuration builder for ManualPoll mode
    /// </summary>
    public class ManualPollConfigurationBuilder : ConfigurationBuilderBase<ManualPollConfiguration>
    {
        internal ManualPollConfigurationBuilder(ConfigCatClientBuilder clientBuilder) : base(clientBuilder) { }

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