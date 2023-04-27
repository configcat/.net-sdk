using System;

namespace ConfigCat.Client.Configuration;

/// <summary>
/// Represents the base class for the polling modes.
/// </summary>
public abstract class PollingMode
{
    private protected PollingMode() { }

    internal abstract string Identifier { get; }
}

/// <summary>
/// AutoPoll configuration settings object for <see cref="ConfigCatClient"/>
/// </summary>
public class AutoPoll : PollingMode
{
    internal override string Identifier => "a";

    /// <summary>
    /// Configuration refresh period.
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
/// LazyLoad configuration settings object for <see cref="ConfigCatClient"/>
/// </summary>
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
/// ManualPoll configuration settings object for <see cref="ConfigCatClient"/>
/// </summary>
public class ManualPoll : PollingMode
{
    internal override string Identifier => "m";
}
