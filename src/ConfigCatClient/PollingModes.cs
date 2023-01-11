using System;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client;

/// <summary>
/// Contains the supported polling modes.
/// </summary>
public static class PollingModes
{
    /// <summary>
    /// Constructs a new auto polling mode.
    /// </summary>
    /// <param name="pollInterval">Configuration refresh period. (Default value is 60 seconds.)</param>
    /// <param name="maxInitWaitTime">Maximum waiting time between initialization and the first config acquisition. (Default value is 5 seconds.)</param>
    /// <returns>The auto polling mode.</returns>
    public static AutoPoll AutoPoll(TimeSpan? pollInterval = null, TimeSpan? maxInitWaitTime = null) =>
        new(pollInterval ?? TimeSpan.FromSeconds(60), maxInitWaitTime ?? TimeSpan.FromSeconds(5));

    /// <summary>
    /// Constructs a new lazy load polling mode.
    /// </summary>
    /// <param name="cacheTimeToLive">Cache time to live value, minimum value is 1 seconds. (Default value is 60 seconds.)</param>
    /// <returns>The lazy load polling mode.</returns>
    public static LazyLoad LazyLoad(TimeSpan? cacheTimeToLive = null) =>
        new(cacheTimeToLive ?? TimeSpan.FromSeconds(60));

    /// <summary>
    /// The manual polling mode.
    /// </summary>
    public static readonly ManualPoll ManualPoll = new();
}
