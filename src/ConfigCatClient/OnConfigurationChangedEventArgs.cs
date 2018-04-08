using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Event arguments for OnConfigurationChanged event
    /// </summary>
    public class OnConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Provides a value to use with events that do not have event data.
        /// </summary>
        public new static readonly OnConfigurationChangedEventArgs Empty = new OnConfigurationChangedEventArgs();
    }
}