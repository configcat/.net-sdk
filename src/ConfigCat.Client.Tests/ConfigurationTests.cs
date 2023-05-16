using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ConfigurationTests
{
    [DataRow(-1L, false)]
    [DataRow(0L, false)]
    [DataRow(999L, false)]
    [DataRow(1000L, true)]
    [DataRow(int.MaxValue, true)]
    [DataRow(int.MaxValue + 1L, false)]
    [DataTestMethod]
    public void AutoPoll_PollIntervalRangeValidation_Works(long pollIntervalMs, bool isValid)
    {
        var pollInterval = TimeSpan.FromMilliseconds(pollIntervalMs);
        Action action = () => PollingModes.AutoPoll(pollInterval: pollInterval);

        if (isValid)
        {
            action();
        }
        else
        {
            var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(action);
            Assert.AreEqual(nameof(pollInterval), ex.ParamName);
            Assert.AreEqual(pollInterval, ex.ActualValue);
        }
    }

    [DataRow(-1L, true)]
    [DataRow(0L, true)]
    [DataRow(int.MaxValue, true)]
    [DataRow(int.MaxValue + 1L, false)]
    [DataTestMethod]
    public void AutoPoll_MaxInitWaitTimeRangeValidation_Works(long maxInitWaitTimeMs, bool isValid)
    {
        var maxInitWaitTime = TimeSpan.FromMilliseconds(maxInitWaitTimeMs);
        Action action = () => PollingModes.AutoPoll(maxInitWaitTime: maxInitWaitTime);

        if (isValid)
        {
            action();
        }
        else
        {
            var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(action);
            Assert.AreEqual(nameof(maxInitWaitTime), ex.ParamName);
            Assert.AreEqual(maxInitWaitTime, ex.ActualValue);
        }
    }

    [DataRow(-1L, false)]
    [DataRow(0L, false)]
    [DataRow(999L, false)]
    [DataRow(1000L, true)]
    [DataRow(int.MaxValue * 1000L, true)]
    [DataRow(int.MaxValue * 1000L + 1L, false)]
    [DataTestMethod]
    public void LazyLoad_CacheTimeToLiveRangeValidation_Works(long cacheTimeToLiveMs, bool isValid)
    {
        var cacheTimeToLive = TimeSpan.FromMilliseconds(cacheTimeToLiveMs);
        Action action = () => PollingModes.LazyLoad(cacheTimeToLive: cacheTimeToLive);

        if (isValid)
        {
            action();
        }
        else
        {
            var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(action);
            Assert.AreEqual(nameof(cacheTimeToLive), ex.ParamName);
            Assert.AreEqual(cacheTimeToLive, ex.ActualValue);
        }
    }
}
