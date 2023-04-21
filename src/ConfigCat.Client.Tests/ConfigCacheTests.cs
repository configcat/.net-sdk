using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestCategory(TestCategories.Integration)]
[TestClass]
public class ConfigCacheTests
{
    private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";
    private static readonly HttpClientHandler SharedHandler = new();

    [TestMethod]
    [DoNotParallelize]
    public async Task ConfigCache_Override_AutoPoll_Works()
    {
        string? cachedConfig = null;
        var configCacheMock = new Mock<IConfigCatCache>();

        configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback<string, string, CancellationToken>((key, config, _) =>
        {
            cachedConfig = config;
        });

        configCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => cachedConfig);

        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Debug);
            options.PollingMode = PollingModes.AutoPoll();
            options.ConfigCache = configCacheMock.Object;
            options.HttpClientHandler = SharedHandler;
        });

        var actual = await client.GetValueAsync("stringDefaultCat", "N/A");

        Assert.AreEqual("Cat", actual);

        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task ConfigCache_Override_ManualPoll_Works()
    {
        string? cachedConfig = null;
        var configCacheMock = new Mock<IConfigCatCache>();
        configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback<string, string, CancellationToken>((key, config, _) =>
        {
            cachedConfig = config;
        });

        configCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => cachedConfig);

        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Debug);
            options.PollingMode = PollingModes.ManualPoll;
            options.ConfigCache = configCacheMock.Object;
            options.HttpClientHandler = SharedHandler;
        });

        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        var actual = await client.GetValueAsync("stringDefaultCat", "N/A");

        Assert.AreEqual("N/A", actual);
        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        await client.ForceRefreshAsync();

        actual = await client.GetValueAsync("stringDefaultCat", "N/A");
        Assert.AreEqual("Cat", actual);
        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task ConfigCache_Override_LazyLoad_Works()
    {
        string? cachedConfig = null;
        var configCacheMock = new Mock<IConfigCatCache>();
        configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback<string, string, CancellationToken>((key, config, _) =>
        {
            cachedConfig = config;
        });

        configCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => cachedConfig);

        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Debug);
            options.PollingMode = PollingModes.LazyLoad();
            options.ConfigCache = configCacheMock.Object;
            options.HttpClientHandler = SharedHandler;
        });

        var actual = await client.GetValueAsync("stringDefaultCat", "N/A");
        Assert.AreEqual("Cat", actual);

        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    [DataTestMethod]
    public async Task ConfigCache_Works(bool isExternal, bool isAsync)
    {
        const string cacheKey = "";

        FakeExternalCache? externalCache = null;
        ConfigCache configCache = isExternal
            ? new ExternalConfigCache(externalCache = new FakeExternalCache(), new Mock<IConfigCatLogger>().Object.AsWrapper())
            : new InMemoryConfigCache();

        // 1. Cache should return the empty config initially
        var cachedConfig = isAsync ? await configCache.GetAsync(cacheKey) : configCache.Get(cacheKey);
        Assert.AreSame(ProjectConfig.Empty, cachedConfig);

        // 2a. When cache is empty, setting an empty config with newer timestamp should overwrite the cache (but only locally!)
        var config2a = ProjectConfig.Empty.With(ProjectConfig.GenerateTimeStamp());
        await WriteCacheAsync(config2a);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config2a, cachedConfig);
        Assert.AreEqual(config2a, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNull(externalCache.CachedValue);
        }

        // 2b. When cache is empty, setting an empty config with equal timestamp shouldn't cause any changes.
        var config2b = config2a.With(config2a.TimeStamp);
        await WriteCacheAsync(config2b);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config2a, cachedConfig);
        Assert.AreEqual(config2a, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNull(externalCache.CachedValue);
        }

        // 2c. When cache is empty, setting an empty config with older timestamp shouldn't cause any changes.
        var config2c = config2a.With(config2a.TimeStamp - TimeSpan.FromSeconds(1));
        await WriteCacheAsync(config2c);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config2a, cachedConfig);
        Assert.AreEqual(config2a, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNull(externalCache.CachedValue);
        }

        // 3. When cache is empty, setting a non-empty config with any (even older) timestamp should overwrite the cache.
        var config3 = ConfigHelper.FromString("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}", "\"ETAG\"", config2c.TimeStamp);
        await WriteCacheAsync(config3);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config3, cachedConfig);
        Assert.AreEqual(config3, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNotNull(externalCache.CachedValue);
        }

        // 4a. When cache is non-empty, setting a non-empty config with an older timestamp shouldn't cause any changes.
        var config4a = config3.With(config3.TimeStamp - TimeSpan.FromSeconds(1));
        await WriteCacheAsync(config4a);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config3, cachedConfig);
        Assert.AreEqual(config3, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNotNull(externalCache.CachedValue);
        }

        // 4b. When cache is non-empty, setting a non-empty config with an equal timestamp and equal etag shouldn't cause any changes.
        var config4b = config3.With(config3.TimeStamp);
        await WriteCacheAsync(config4b);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config3, cachedConfig);
        Assert.AreEqual(config3, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNotNull(externalCache.CachedValue);
        }

        // 4c. When cache is non-empty, setting a non-empty config with an equal timestamp but not equal etag should overwrite the cache.
        //     (In such edge cases that one wins who executes update later.)
        var config4c = new ProjectConfig(config3.ConfigJson, config3.Config, config3.TimeStamp, "\"ETAG2\"");
        await WriteCacheAsync(config4c);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config4c, cachedConfig);
        Assert.AreEqual(config4c, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNotNull(externalCache.CachedValue);
        }

        // 4d. When cache is non-empty, setting a non-empty config with a newer timestamp and any (even equal) etag should overwrite the cache.
        var config4d = config4c.With(config4c.TimeStamp + TimeSpan.FromSeconds(1));
        await WriteCacheAsync(config4d);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config4d, cachedConfig);
        Assert.AreEqual(config4d, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNotNull(externalCache.CachedValue);
        }

        Task<ProjectConfig> ReadCacheAsync()
        {
            return isAsync ? configCache.GetAsync(cacheKey) : Task.FromResult(configCache.Get(cacheKey));
        }

        Task WriteCacheAsync(ProjectConfig config)
        {
            if (isAsync)
            {
                return configCache.SetAsync(cacheKey, config);
            }
            else
            {
                configCache.Set(cacheKey, config);
                return Task.FromResult(0);
            }
        }
    }

    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    [DataTestMethod]
    public async Task ConfigCache_ShouldNeverOverwriteCacheWithOlderEntry(bool isExternal, bool isEmpty)
    {
        const string cacheKey = "";

        FakeExternalCache? externalCache = null;
        ConfigCache configCache = isExternal
            ? new ExternalConfigCache(externalCache = new FakeExternalCache(), new Mock<IConfigCatLogger>().Object.AsWrapper())
            : new InMemoryConfigCache();

        long timeStampTicks = ProjectConfig.GenerateTimeStamp().Ticks;

        var config = isEmpty
            ? ProjectConfig.Empty
            : ConfigHelper.FromString("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}", "\"ETAG\"", DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(4));

        var outOfOrderWrite = false;

        var thread1 = Task.Factory.StartNew(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var cachedConfigBefore = configCache.Get(cacheKey);

                var timeStamp = ProjectConfig.GenerateTimeStamp();
                Volatile.Write(ref timeStampTicks, timeStamp.Ticks);

                configCache.Set(cacheKey, config.With(timeStamp));

                var cachedConfigAfter = configCache.Get(cacheKey);

                outOfOrderWrite = outOfOrderWrite || cachedConfigAfter.TimeStamp < cachedConfigBefore.TimeStamp;
            }
        }, TaskCreationOptions.LongRunning);

        var thread2 = Task.Factory.StartNew(() =>
        {
            var random = new Random();

            while (!cts.Token.IsCancellationRequested)
            {
                var offset = TimeSpan.FromSeconds(random.Next(3) - 1);
                var timeStamp = new DateTime(Volatile.Read(ref timeStampTicks), DateTimeKind.Utc);

                configCache.Set(cacheKey, config.With(timeStamp + offset));
            }
        }, TaskCreationOptions.LongRunning);

        await Task.WhenAll(thread1, thread2);

        Assert.IsFalse(outOfOrderWrite);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task ConfigCache_ShouldHandleWhenExternalCacheFails(bool isAsync)
    {
        const string cacheKey = "";

        var logLevels = new List<LogLevel>();
        var logEventIds = new List<LogEventId>();
        var logExceptions = new List<Exception>();

        var loggerMock = new Mock<IConfigCatLogger>();
        loggerMock.Setup(l => l.LogLevel).Returns(LogLevel.Warning);
        loggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()));

        var externalCache = new FaultyFakeExternalCache();
        var configCache = new ExternalConfigCache(externalCache, loggerMock.Object.AsWrapper());

        // 1. Initial read should return the empty config.
        var cachedConfig = isAsync ? await configCache.GetAsync(cacheKey) : configCache.Get(cacheKey);

        Assert.AreEqual(ProjectConfig.Empty, cachedConfig);

        loggerMock.Verify(l => l.Log(LogLevel.Error, 2200, ref It.Ref<FormattableLogMessage>.IsAny, It.Is<Exception>(ex => ex is ApplicationException)), Times.Once);

        // 2. Set should overwrite the local cache and log the error.

        loggerMock.Invocations.Clear();
        var config = ConfigHelper.FromString("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}", "\"ETAG\"", ProjectConfig.GenerateTimeStamp());

        if (isAsync)
        {
            await configCache.SetAsync(cacheKey, config);
        }
        else
        {
            configCache.Set(cacheKey, config);
        }

        Assert.AreEqual(config, configCache.LocalCachedConfig);

        loggerMock.Verify(l => l.Log(LogLevel.Error, 2201, ref It.Ref<FormattableLogMessage>.IsAny, It.Is<Exception>(ex => ex is ApplicationException)), Times.Once);

        // 3. Get should log the error and return the local cache which was set previously.

        loggerMock.Invocations.Clear();
        cachedConfig = isAsync ? await configCache.GetAsync(cacheKey) : configCache.Get(cacheKey);

        Assert.AreEqual(config, cachedConfig);

        loggerMock.Verify(l => l.Log(LogLevel.Error, 2200, ref It.Ref<FormattableLogMessage>.IsAny, It.Is<Exception>(ex => ex is ApplicationException)), Times.Once);
    }

    private sealed class FakeExternalCache : IConfigCatCache
    {
        public volatile string? CachedValue = null;

        public string? Get(string key) => this.CachedValue;

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Get(key));

        public void Set(string key, string value) => this.CachedValue = value;

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Set(key, value);
            return Task.FromResult(0);
        }
    }

    private sealed class FaultyFakeExternalCache : IConfigCatCache
    {
        public string? Get(string key) => throw new ApplicationException("Operation failed :(");

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Get(key));

        public void Set(string key, string value) => throw new ApplicationException("Operation failed :(");

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Set(key, value);
            return Task.FromResult(0);
        }
    }
}
