using System;

namespace ConfigCat.Client.Configuration;

/// <summary>
/// Represents the ConfigCat SDK's configuration options.
/// </summary>
public class ConfigCatClientOptions : ConfigurationBase, IProvidesHooks
{
    internal Hooks Hooks { get; } = new Hooks();

    /// <summary>
    /// The polling mode. Defaults to auto polling.
    /// </summary>
    public PollingMode PollingMode { get; set; } = PollingModes.AutoPoll();

    /// <summary>
    /// Indicates whether the client should be initialized to offline mode or not. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Offline { get; set; }

    /// <inheritdoc/>
    public event EventHandler ClientReady
    {
        add { Hooks.ClientReady += value; }
        remove { Hooks.ClientReady -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated
    {
        add { Hooks.FlagEvaluated += value; }
        remove { Hooks.FlagEvaluated -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigChangedEventArgs> ConfigChanged
    {
        add { Hooks.ConfigChanged += value; }
        remove { Hooks.ConfigChanged -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigCatClientErrorEventArgs> Error
    {
        add { Hooks.Error += value; }
        remove { Hooks.Error -= value; }
    }

    internal override void Validate()
    {
        PollingMode.Validate();
        base.Validate();
    }
}
