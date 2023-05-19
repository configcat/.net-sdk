using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides data for the <see cref="ConfigCatClient.ConfigChanged"/> event.
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    internal ConfigChangedEventArgs(IConfig newConfig)
    {
        NewConfig = newConfig;
    }

    /// <summary>
    /// The new <see cref="IConfig"/> object.
    /// </summary>
    public IConfig NewConfig { get; }
}
