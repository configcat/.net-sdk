using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Base configuration builder
    /// </summary>    
    [Obsolete("This class is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
    public abstract class ConfigurationBuilderBase<T> where T : ConfigurationBase, new()
    {
#pragma warning disable CS1591,CS0618
        protected readonly T configuration;

        internal ConfigurationBuilderBase(ConfigCatClientBuilder clientBuilder)
        {
            this.configuration = new T
            {
                SdkKey = clientBuilder.SdkKey,
                Logger = clientBuilder.Logger,
                DataGovernance = clientBuilder.DataGovernance
            };
        }
    }
}