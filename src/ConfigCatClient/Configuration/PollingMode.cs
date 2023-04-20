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
    /// Maximum waiting time between initialization and the first config acquisition. (Default value is 5 seconds.)
    /// </summary>
    public TimeSpan MaxInitWaitTime { get; }

    internal AutoPoll(TimeSpan pollInterval, TimeSpan maxInitWaitTime)
    {
        PollInterval = pollInterval > TimeSpan.Zero
            ? pollInterval
            : throw new ArgumentOutOfRangeException(nameof(pollInterval), "Value must be greater than zero.");

        MaxInitWaitTime = maxInitWaitTime;
    }
}

/// <summary>
/// LazyLoad configuration settings object for <see cref="ConfigCatClient"/>
/// </summary>
public class LazyLoad : PollingMode
{
    internal override string Identifier => "l";

    /// <summary>
    /// Cache time to live value, minimum value is 1 seconds. (Default value is 60 seconds.)     
    /// </summary>
    public TimeSpan CacheTimeToLive { get; }

    internal LazyLoad(TimeSpan cacheTimeToLive)
    {
        CacheTimeToLive = cacheTimeToLive >= TimeSpan.FromSeconds(1)
            ? cacheTimeToLive
            : throw new ArgumentOutOfRangeException(nameof(cacheTimeToLive), "Value must be greater than or equal to 1 seconds.");
    }
}

/// <summary>
/// ManualPoll configuration settings object for <see cref="ConfigCatClient"/>
/// </summary>
public class ManualPoll : PollingMode
{
    internal override string Identifier => "m";
}
