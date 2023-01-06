using System;

namespace ConfigCat.Client;

/// <summary>
/// LazyLoad configuration settings object for <see cref="ConfigCatClient"/>
/// </summary>
[Obsolete("This class is obsolete and will be removed from the public API in a future major version. Please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.LazyLoad(); })' format.")]
public class LazyLoadConfiguration : ConfigurationBase
{
    /// <summary>
    /// Cache time to live value in seconds, minimum value is 1. (Default value is 60.)     
    /// </summary>
    public uint CacheTimeToLiveSeconds { get; set; } = 60;

    internal override void Validate()
    {
        base.Validate();

        if (CacheTimeToLiveSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(CacheTimeToLiveSeconds), "Value must be greater than or equal to 1 seconds.");
        }
    }
}
