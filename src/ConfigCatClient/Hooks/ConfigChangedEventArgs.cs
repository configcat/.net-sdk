using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides data for the <see cref="ConfigCatClient.ConfigChanged"/> event.
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    // TODO: in what form should we provide the new config to user?
    internal ConfigChangedEventArgs(object newConfig)
    {
        NewConfig = newConfig;
    }

    /// <summary>
    /// The new <see cref="ProjectConfig"/> object.
    /// </summary>
    public object NewConfig { get; }
}
