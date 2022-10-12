using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// ManualPoll configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    [Obsolete("This class is obsolete and will be removed from the public API in a future major version. Please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.ManualPoll(); })' format.")]
    public class ManualPollConfiguration : ConfigurationBase { }
}