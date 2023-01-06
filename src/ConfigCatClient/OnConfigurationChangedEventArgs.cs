using System;

namespace ConfigCat.Client;

/// <summary>
/// Event arguments for OnConfigurationChanged event
/// </summary>
public class OnConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Provides a value to use with events that do not have event data.
    /// </summary>
    public static new readonly OnConfigurationChangedEventArgs Empty = new();
}
