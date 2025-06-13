using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides data for the <see cref="ConfigCatClient.ConfigChanged"/> event.
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    internal ConfigChangedEventArgs(Config newConfig)
    {
        NewConfig = newConfig;
    }

    /// <summary>
    /// The new <see cref="Config"/> object.
    /// </summary>
    public Config NewConfig { get; }
}
