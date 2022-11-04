using System;

namespace ConfigCat.Client.Configuration
{
    /// <summary>
    /// Represents the base class for the polling modes.
    /// </summary>
    public abstract class PollingMode
    {
        internal abstract string Identifier { get; }

        internal abstract void Validate();
    }

    /// <summary>
    /// AutoPoll configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    public class AutoPoll : PollingMode
    {
        internal override string Identifier => "a";

        /// <summary>
        /// Configuration refresh period.
        /// </summary>
        public TimeSpan PollInterval { get; }

        /// <summary>
        /// Maximum waiting time between initialization and the first config acquisition. (Default value is 5 seconds.)
        /// </summary>
        public TimeSpan MaxInitWaitTime { get; }

        /// <summary>
        /// <see cref="OnConfigurationChanged"/> raised when the configuration was updated
        /// </summary>
        [Obsolete("This event is obsolete and will be removed from the public API in a future major version. Please use the 'ConfigCatClientOptions.ConfigChanged' event instead.")]
        public event OnConfigurationChangedEventHandler OnConfigurationChanged;

        internal AutoPoll(TimeSpan pollInterval, TimeSpan maxInitWaitTime)
        {
            PollInterval = pollInterval;
            MaxInitWaitTime = maxInitWaitTime;
        }

        internal override void Validate()
        {
            if (PollInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(this.PollInterval), "Value must be greater than zero.");
            }
        }

        internal void RaiseOnConfigurationChanged(object sender, OnConfigurationChangedEventArgs args) =>
            this.OnConfigurationChanged?.Invoke(sender, args);
    }

    /// <summary>
    /// LazyLoad configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    public class LazyLoad : PollingMode
    {
        internal override string Identifier => "l";

        /// <summary>
        /// Cache time to live value, minimum value is 1 seconds. (Default value is 60 seconds.)     
        /// </summary>
        public TimeSpan CacheTimeToLive { get; }

        internal LazyLoad(TimeSpan cacheTimeToLive)
        {
            CacheTimeToLive = cacheTimeToLive;
        }

        internal override void Validate()
        {
            if (this.CacheTimeToLive < TimeSpan.FromSeconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(this.CacheTimeToLive), "Value must be greater than or equal to 1 seconds.");
            }
        }
    }

    /// <summary>
    /// ManualPoll configuration settings object for <see cref="ConfigCatClient"/>
    /// </summary>
    public class ManualPoll : PollingMode
    {
        internal override string Identifier => "m";

        internal override void Validate() { }
    }
}
