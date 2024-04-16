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
    event EventHandler? ClientReady;

    /// <summary>
    /// Occurs after the value of a feature flag of setting has been evaluated.
    /// </summary>
    event EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated;

    /// <summary>
    /// Occurs after attempting to refresh the locally cached config by fetching the latest version from the remote server.
    /// </summary>
    event EventHandler<ConfigFetchedEventArgs>? ConfigFetched;

    /// <summary>
    /// Occurs after the locally cached config has been updated to a newer version.
    /// </summary>
    event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    /// <summary>
    /// Occurs in the case of a failure in the client.
    /// </summary>
    event EventHandler<ConfigCatClientErrorEventArgs>? Error;
}
