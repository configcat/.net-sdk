namespace ConfigCat.Client.Configuration
{
    /// <summary>
    /// Represents the ConfigCat SDK's configuration options.
    /// </summary>
    public class ConfigCatClientOptions : ConfigurationBase
    {
        /// <summary>
        /// The polling mode. Defaults to auto polling.
        /// </summary>
        public PollingMode PollingMode { get; set; } = PollingModes.AutoPoll();

        /// <summary>
        /// Indicates whether the client should be initialized to offline mode or not. Defaults to <see langword="false"/>.
        /// </summary>
        public bool Offline { get; set; }

        internal override void Validate()
        {
            PollingMode.Validate();
            base.Validate();
        }
    }
}
