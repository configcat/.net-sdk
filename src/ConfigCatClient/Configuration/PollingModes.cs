using System;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client;

/// <summary>
/// Provides static factory methods for the supported polling modes.
/// </summary>
public static class PollingModes
{
    /// <summary>
    /// Creates an instance of the <see cref="Configuration.AutoPoll"/> class with the specified settings.
    /// </summary>
    /// <param name="pollInterval">
    /// Specifies how frequently the internally cached config will be updated by synchronizing with
    /// the external cache and/or by fetching the latest version from the ConfigCat CDN.<br/>
    /// (Default value is 60 seconds. Minimum value is 1 second. Maximum value is <see cref="int.MaxValue"/> milliseconds.)
    /// </param>
    /// <param name="maxInitWaitTime">
    /// Maximum waiting time before reporting the ready state, i.e. emitting the <see cref="IProvidesHooks.ClientReady"/> event.<br/>
    /// (Default value is 5 seconds. Maximum value is <see cref="int.MaxValue"/> milliseconds. Negative values mean infinite waiting.)
    /// </param>
    /// <returns>The new <see cref="Configuration.AutoPoll"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pollInterval"/> is outside the allowed range.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxInitWaitTime"/> is outside the allowed range.</exception>
    public static AutoPoll AutoPoll(TimeSpan? pollInterval = null, TimeSpan? maxInitWaitTime = null) =>
        new(pollInterval ?? TimeSpan.FromSeconds(60), maxInitWaitTime ?? TimeSpan.FromSeconds(5));

    /// <summary>
    /// Creates an instance of the <see cref="Configuration.LazyLoad"/> class with the specified settings.
    /// </summary>
    /// <param name="cacheTimeToLive">
    /// Cache time to live value.
    /// Specifies how long the cached config can be used before updating it again by fetching the latest version from the ConfigCat CDN.<br/>
    /// (Default value is 60 seconds. Minimum value is 1 second. Maximum value is <see cref="int.MaxValue"/> seconds.)
    /// </param>
    /// <returns>The new <see cref="Configuration.LazyLoad"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cacheTimeToLive"/> is outside the allowed range.</exception>
    public static LazyLoad LazyLoad(TimeSpan? cacheTimeToLive = null) =>
        new(cacheTimeToLive ?? TimeSpan.FromSeconds(60));

    /// <summary>
    /// Provides an instance of the <see cref="Configuration.ManualPoll"/> class.
    /// </summary>
    public static readonly ManualPoll ManualPoll = new();
}
