namespace ConfigCat.Client
{
    /// <summary>
    /// Base configuration builder
    /// </summary>    
    public abstract class ConfigurationBuilderBase<T> where T : ConfigurationBase, new()
    {
#pragma warning disable CS1591
        protected readonly T configuration;

        internal ConfigurationBuilderBase(ConfigCatClientBuilder clientBuilder)
        {
            this.configuration = new T
            {
                SdkKey = clientBuilder.SdkKey,
                Logger = clientBuilder.Logger                
            };
        }
    }
}