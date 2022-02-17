using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Represents the method that will handle an OnConfigurationChangedEvent with <see cref="OnConfigurationChangedEventArgs"/> arguments
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="eventArgs">Arguments of the event</param>
    public delegate void OnConfigurationChangedEventHandler(object sender, OnConfigurationChangedEventArgs eventArgs);

    /// <summary>
    /// AutoPoll configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    [Obsolete("Please use the 'new ConfigCatClient(options => { options.PollingMode = PollingMode.AutoPoll(); })' format.")]
    public class AutoPollConfiguration : ConfigurationBase
    {
        /// <summary>
        /// Configuration refresh period (Default value is 60.)
        /// </summary>
        public uint PollIntervalSeconds { get; set; } = 60;
        
        /// <summary>
        /// Maximum waiting time between initialization and the first config acquisition in seconds. (Default value is 5.)
        /// </summary>
        public uint MaxInitWaitTimeSeconds { get; set; } = 5;


        /// <summary>
        /// <see cref="OnConfigurationChanged"/> raised when the configuration was updated
        /// </summary>
        public event OnConfigurationChangedEventHandler OnConfigurationChanged;

        internal void RaiseOnConfigurationChanged(object sender, OnConfigurationChangedEventArgs args) => 
            this.OnConfigurationChanged?.Invoke(sender, args);

        internal override void Validate()
        {
            base.Validate();

            if (PollIntervalSeconds == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(this.PollIntervalSeconds), "Value must be greater than zero.");
            }
        }
    }
}
