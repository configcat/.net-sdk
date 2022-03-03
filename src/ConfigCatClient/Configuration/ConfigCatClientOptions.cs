namespace ConfigCat.Client.Configuration
{
    /// <summary>
    /// Represents the ConfigCat SDK's configuration options.
    /// </summary>
    public class ConfigCatClientOptions : ConfigurationBase
    {
        internal PollingMode PollingMode { get; set; } = PollingModes.AutoPoll();

        internal override void Validate()
        {
            PollingMode.Validate();
            base.Validate();
        }
    }
}
