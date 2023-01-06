using System;

namespace ConfigCat.Client;

/// <summary>
/// Defines hooks (events) for providing notifications of <see cref="ConfigCatClient"/>'s actions.
/// </summary>
public interface IProvidesHooks
{
    /// <summary>
    /// Occurs when the client is ready to provide the actual value of feature flags or settings.
    /// </summary>
    event EventHandler ClientReady;

    /// <summary>
    /// Occurs after the value of a feature flag of setting has been evaluated.
    /// </summary>
    event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated;

    /// <summary>
    /// Occurs after the configuration has been updated.
    /// </summary>
    event EventHandler<ConfigChangedEventArgs> ConfigChanged;

    /// <summary>
    /// Occurs in the case of a failure in the client.
    /// </summary>
    event EventHandler<ConfigCatClientErrorEventArgs> Error;
}
