using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// ManualPoll configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    [Obsolete("Please use the 'new ConfigCatClient(options => { options.PollingMode = PollingModes.ManualPoll; })' format.")]
    public class ManualPollConfiguration : ConfigurationBase { }
}