using System;

namespace ConfigCat.Client;

/// <summary>
/// Defines hooks (events) for providing notifications of <see cref="ConfigCatClient"/>'s actions.
/// </summary>
public interface IProvidesHooks
{
    /// <summary>
    /// Occurs when the client reaches the ready state, i.e. completes initialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ready state is reached as soon as the initial sync with the external cache (if any) completes.
    /// If this does not produce up-to-date config data, and the client is online (i.e. HTTP requests are allowed),
    /// the first config fetch operation is also awaited in Auto Polling mode before ready state is reported.
    /// </para>
    /// <para>
    /// That is, reaching the ready state usually means the client is ready to evaluate feature flags and settings.
    /// However, please note that this is not guaranteed. In case of initialization failure or timeout, the internal cache
    /// may be empty or expired even after the ready state is reported. You can verify this by checking <see cref="ClientReadyEventArgs.CacheState"/>.
    /// </para>
    /// </remarks>
    event EventHandler<ClientReadyEventArgs>? ClientReady;

    /// <summary>
    /// Occurs after the value of a feature flag of setting has been evaluated.
    /// </summary>
    event EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated;

    /// <summary>
    /// Occurs after attempting to update the cached config by fetching the latest version from the ConfigCat CDN.
    /// </summary>
    event EventHandler<ConfigFetchedEventArgs>? ConfigFetched;

    /// <summary>
    /// Occurs after the internally cached config has been updated to a newer version, either as a result of synchronization
    /// with the external cache, or as a result of fetching a newer version from the ConfigCat CDN.
    /// </summary>
    event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    /// <summary>
    /// Occurs in the case of a failure in the client.
    /// </summary>
    event EventHandler<ConfigCatClientErrorEventArgs>? Error;
}
