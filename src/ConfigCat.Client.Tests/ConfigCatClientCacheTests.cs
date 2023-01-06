using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConfigCat.Client.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ConfigCatClientCacheTests
{
    [TestMethod]
    public void GetOrCreate_ReturnsSharedInstanceWhenCachedInstanceIsAlive()
    {
        // Arrange

        const string sdkKey = "123";

        var options = new ConfigCatClientOptions
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SdkKey = sdkKey,
#pragma warning restore CS0618 // Type or member is obsolete
            PollingMode = PollingModes.ManualPoll,
        };

        var cache = new ConfigCatClientCache();

        // Act

        var client1 = cache.GetOrCreate(sdkKey, options, out var instanceAlreadyCreated1);
        var client2 = cache.GetOrCreate(sdkKey, options, out var instanceAlreadyCreated2);

        // Assert

        Assert.AreEqual(1, cache.GetAliveCount(out var cacheSize));
        Assert.AreEqual(1, cacheSize);

        Assert.IsNotNull(client1);
        Assert.IsFalse(instanceAlreadyCreated1);

        Assert.IsNotNull(client2);
        Assert.IsTrue(instanceAlreadyCreated2);

        Assert.AreSame(client1, client2);
    }

    [TestMethod]
    public void GetOrCreate_ReturnsNewInstanceAfterCachedInstanceIsCollected()
    {
        // Arrange

        const string sdkKey = "123";

        var options = new ConfigCatClientOptions
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SdkKey = sdkKey,
#pragma warning restore CS0618 // Type or member is obsolete
            PollingMode = PollingModes.ManualPoll,
        };

        var cache = new ConfigCatClientCache();

        // Act

        [MethodImpl(MethodImplOptions.NoInlining)]
        WeakReference<ConfigCatClient> CreateClient(string sdkKey, ConfigCatClientOptions options, out bool instanceAlreadyCreated1)
        {
            var client = cache.GetOrCreate(sdkKey, options, out instanceAlreadyCreated1);
            return new WeakReference<ConfigCatClient>(client);
        }

        var client1 = CreateClient(sdkKey, options, out var instanceAlreadyCreated1);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        var client2 = cache.GetOrCreate(sdkKey, options, out var instanceAlreadyCreated2);

        // Assert

        Assert.AreEqual(1, cache.GetAliveCount(out var cacheSize));
        Assert.AreEqual(1, cacheSize);

        Assert.IsFalse(client1.TryGetTarget(out _));
        Assert.IsFalse(instanceAlreadyCreated1);

        Assert.IsNotNull(client2);
        Assert.IsFalse(instanceAlreadyCreated2);
    }

    [TestMethod]
    public void Remove_WhenCachedInstanceIsAlive()
    {
        // Arrange

        const string sdkKey = "123";

        var options = new ConfigCatClientOptions
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SdkKey = sdkKey,
#pragma warning restore CS0618 // Type or member is obsolete
            PollingMode = PollingModes.ManualPoll,
        };

        var cache = new ConfigCatClientCache();

        var client1 = cache.GetOrCreate(sdkKey, options, out var instanceAlreadyCreated1);

        // Act

        var success = cache.Remove(sdkKey, instanceToRemove: client1);

        // Assert

        Assert.IsTrue(success);
        Assert.AreEqual(0, cache.GetAliveCount(out var cacheSize));
        Assert.AreEqual(0, cacheSize);

        Assert.IsNotNull(client1);
        Assert.IsFalse(instanceAlreadyCreated1);
    }

    [TestMethod]
    public void Remove_WhenCachedInstanceIsCollected()
    {
        // Arrange

        const string sdkKey = "123";

        var options = new ConfigCatClientOptions
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SdkKey = sdkKey,
#pragma warning restore CS0618 // Type or member is obsolete
            PollingMode = PollingModes.ManualPoll,
        };

        var cache = new ConfigCatClientCache();

        [MethodImpl(MethodImplOptions.NoInlining)]
        WeakReference<ConfigCatClient> CreateClient(string sdkKey, ConfigCatClientOptions options, out bool instanceAlreadyCreated1)
        {
            var client = cache.GetOrCreate(sdkKey, options, out instanceAlreadyCreated1);
            return new WeakReference<ConfigCatClient>(client);
        }

        var client1 = CreateClient(sdkKey, options, out var instanceAlreadyCreated1);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Act

        var success = cache.Remove(sdkKey, instanceToRemove: null);

        // Assert

        Assert.IsFalse(success);
        Assert.AreEqual(0, cache.GetAliveCount(out var cacheSize));
        Assert.AreEqual(0, cacheSize);

        Assert.IsFalse(client1.TryGetTarget(out _));
        Assert.IsFalse(instanceAlreadyCreated1);
    }

    [TestMethod]
    public void Remove_WhenCachedInstanceIsNotAvailable()
    {
        // Arrange

        const string sdkKey = "123";

        var cache = new ConfigCatClientCache();

        // Act

        var success = cache.Remove(sdkKey, instanceToRemove: null);

        // Assert

        Assert.IsFalse(success);
        Assert.AreEqual(0, cache.GetAliveCount(out var cacheSize));
        Assert.AreEqual(0, cacheSize);
    }

    [TestMethod]
    public void Clear()
    {
        // Arrange

        const string sdkKey1 = "123";
        const string sdkKey2 = "456";

        var options1 = new ConfigCatClientOptions
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SdkKey = sdkKey1,
#pragma warning restore CS0618 // Type or member is obsolete
            PollingMode = PollingModes.ManualPoll,
        };

        var options2 = new ConfigCatClientOptions
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SdkKey = sdkKey2,
#pragma warning restore CS0618 // Type or member is obsolete
            PollingMode = PollingModes.ManualPoll,
        };

        var cache = new ConfigCatClientCache();

        [MethodImpl(MethodImplOptions.NoInlining)]
        WeakReference<ConfigCatClient> CreateClient(string sdkKey, ConfigCatClientOptions options, out bool instanceAlreadyCreated1, out int cacheCount)
        {
            var client = cache.GetOrCreate(sdkKey, options, out instanceAlreadyCreated1);
            cacheCount = cache.GetAliveCount();
            return new WeakReference<ConfigCatClient>(client);
        }

        var client1 = cache.GetOrCreate(sdkKey1, options1, out var instanceAlreadyCreated1);

        var client2 = CreateClient(sdkKey2, options2, out var instanceAlreadyCreated2, out var cacheCountBefore);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Act

        cache.Clear(out var removedInstances);

        // Assert

        Assert.AreEqual(2, cacheCountBefore);
        Assert.AreEqual(0, cache.GetAliveCount(out var cacheSize));
        Assert.AreEqual(0, cacheSize);

        Assert.IsNotNull(client1);
        Assert.IsFalse(instanceAlreadyCreated1);

        Assert.IsFalse(client2.TryGetTarget(out _));
        Assert.IsFalse(instanceAlreadyCreated2);

        CollectionAssert.AreEqual(new[] { client1 }, removedInstances);
    }
}
