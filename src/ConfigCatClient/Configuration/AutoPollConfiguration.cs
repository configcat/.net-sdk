using System;

namespace ConfigCat.Client.Configuration
{
    /// <summary>
    /// AutoPoll configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    public class AutoPollConfiguration : ConfigurationBase
    {
        /// <summary>
        /// Configuration refresh period (Default value is 60.)
        /// </summary>
        public uint PollIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Maximum waiting time between initialization and the first config acquisition in secconds. (Default value is 5.)
        /// </summary>
        public uint MaxInitWaitTimeSeconds { get; set; } = 5;

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