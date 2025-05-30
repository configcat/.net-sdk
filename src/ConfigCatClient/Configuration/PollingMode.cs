using System;

namespace ConfigCat.Client.Configuration;

/// <summary>
/// Defines the base class for polling modes.
/// </summary>
public abstract class PollingMode
{
    private protected PollingMode() { }

    internal abstract string Identifier { get; }
}

/// <summary>
/// Represents the Auto Polling mode along with its settings.
/// </summary>
/// <remarks>
/// In this mode, the ConfigCat SDK downloads the latest config data automatically and stores it in the cache.
/// </remarks>
public class AutoPoll : PollingMode
{
    internal override string Identifier => "a";

    /// <summary>
    /// Config refresh interval.
    /// </summary>
    public TimeSpan PollInterval { get; }

    /// <summary>
    /// Maximum waiting time between initialization and the first config acquisition.
    /// </summary>
    public TimeSpan MaxInitWaitTime { get; }

    internal AutoPoll(TimeSpan pollInterval, TimeSpan maxInitWaitTime)
    {
        var minPollInterval = TimeSpan.FromSeconds(1);
        var maxTimerInterval = TimeSpan.FromMilliseconds(int.MaxValue);

        PollInterval = minPollInterval <= pollInterval && pollInterval <= maxTimerInterval
            ? pollInterval
            : throw new ArgumentOutOfRangeException(nameof(pollInterval), pollInterval, $"Value must be between {minPollInterval} and {maxTimerInterval}.");

        MaxInitWaitTime = maxInitWaitTime <= maxTimerInterval
            ? maxInitWaitTime
            : throw new ArgumentOutOfRangeException(nameof(maxInitWaitTime), maxInitWaitTime, $"Value must be less than or equal to {maxTimerInterval}.");
    }
}

/// <summary>
/// Represents the Lazy Loading mode along with its settings.
/// </summary>
/// <remarks>
/// In this mode, the ConfigCat SDK downloads the latest config data only if it is not present in the cache,
/// or if it is but has expired.
/// </remarks>
public class LazyLoad : PollingMode
{
    internal override string Identifier => "l";

    /// <summary>
    /// Cache time to live value.
    /// </summary>
    public TimeSpan CacheTimeToLive { get; }

    internal LazyLoad(TimeSpan cacheTimeToLive)
    {
        var minCacheTimeToLive = TimeSpan.FromSeconds(1);
        var maxCacheTimeToLive = TimeSpan.FromSeconds(int.MaxValue);

        CacheTimeToLive = minCacheTimeToLive <= cacheTimeToLive && cacheTimeToLive <= maxCacheTimeToLive
            ? cacheTimeToLive
            : throw new ArgumentOutOfRangeException(nameof(cacheTimeToLive), cacheTimeToLive, $"Value must be between {minCacheTimeToLive} and {maxCacheTimeToLive}.");
    }
}

/// <summary>
/// Represents the Manual Polling mode.
/// </summary>
/// <remarks>
/// In this mode, the ConfigCat SDK will not download the config data automatically. You need to update the cache
/// manually, by calling <see cref="ConfigCatClient.ForceRefreshAsync"/>.
/// </remarks>
public class ManualPoll : PollingMode
{
    internal override string Identifier => "m";
}
