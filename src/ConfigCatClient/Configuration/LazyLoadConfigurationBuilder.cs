namespace ConfigCat.Client
{
    /// <summary>
    /// Configuration builder for LazyLoad mode
    /// </summary>
    public class LazyLoadConfigurationBuilder : ConfigurationBuilderBase<LazyLoadConfiguration>
    {
        internal LazyLoadConfigurationBuilder(ConfigCatClientBuilder clientBuilder) : base(clientBuilder) { }

        /// <summary>
        /// Cache time to live value in seconds, minimum value is 1.
        /// </summary>
        /// <param name="cacheTimeToLiveSeconds"></param>        
        public LazyLoadConfigurationBuilder WithCacheTimeToLiveSeconds(uint cacheTimeToLiveSeconds)
        {
            this.configuration.CacheTimeToLiveSeconds = cacheTimeToLiveSeconds;

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