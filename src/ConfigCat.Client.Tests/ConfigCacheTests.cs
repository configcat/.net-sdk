using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Tests.Fakes;
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
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        var actual = await client.GetValueAsync("stringDefaultCat", "N/A");

        Assert.AreEqual("N/A", actual);
        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        await client.ForceRefreshAsync();

        actual = await client.GetValueAsync("stringDefaultCat", "N/A");
        Assert.AreEqual("Cat", actual);
        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
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

        // 2. When cache is empty, setting an empty config with newer timestamp should overwrite the cache (but only locally!)
        var config2 = ProjectConfig.Empty.With(ProjectConfig.GenerateTimeStamp());
        await WriteCacheAsync(config2);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config2, cachedConfig);
        Assert.AreEqual(config2, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNull(externalCache.CachedValue);
        }

        // 3. When cache is empty, setting a non-empty config with any (even older) timestamp should overwrite the cache.
        var config3 = ConfigHelper.FromString("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}", "\"ETAG\"", config2.TimeStamp - TimeSpan.FromSeconds(1));
        await WriteCacheAsync(config3);
        cachedConfig = await ReadCacheAsync();

        Assert.AreSame(config3, cachedConfig);
        Assert.AreEqual(config3, configCache.LocalCachedConfig);
        if (externalCache is not null)
        {
            Assert.IsNotNull(externalCache.CachedValue);
        }

        Task<ProjectConfig> ReadCacheAsync()
        {
            return isAsync ? configCache.GetAsync(cacheKey).AsTask() : Task.FromResult(configCache.Get(cacheKey));
        }

        Task WriteCacheAsync(ProjectConfig config)
        {
            if (isAsync)
            {
                return configCache.SetAsync(cacheKey, config).AsTask();
            }
            else
            {
                configCache.Set(cacheKey, config);
                return Task.FromResult(0);
            }
        }
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

    [DataRow("configcat-sdk-1/TEST_KEY-0123456789012/1234567890123456789012", "f83ba5d45bceb4bb704410f51b704fb6dfa19942")]
    [DataRow("configcat-sdk-1/TEST_KEY2-123456789012/1234567890123456789012", "da7bfd8662209c8ed3f9db96daed4f8d91ba5876")]
    [DataTestMethod]
    public void CacheKeyGeneration_ShouldBePlatformIndependent(string sdkKey, string expectedCacheKey)
    {
        Assert.AreEqual(expectedCacheKey, ConfigCatClient.GetCacheKey(sdkKey));
    }

    private const string PayloadTestConfigJson = "{\"p\":{\"u\":\"https://cdn-global.configcat.com\",\"r\":0,\"s\":\"FUkC6RADjzF0vXrDSfJn7BcEBag9afw1Y6jkqjMP9BA=\"},\"f\":{\"testKey\":{\"t\":1,\"v\":{\"s\":\"testValue\"}}}}";
    [DataRow(PayloadTestConfigJson, "2023-06-14T15:27:15.8440000Z", "test-etag", "1686756435844\ntest-etag\n" + PayloadTestConfigJson)]
    [DataTestMethod]
    public void CachePayloadSerialization_ShouldBePlatformIndependent(string configJson, string timeStamp, string httpETag, string expectedPayload)
    {
        var timeStampDateTime = DateTimeOffset.ParseExact(timeStamp, "o", CultureInfo.InvariantCulture).UtcDateTime;
        var pc = new ProjectConfig(configJson, Config.Deserialize(configJson.AsMemory()), timeStampDateTime, httpETag);

        Assert.AreEqual(expectedPayload, ProjectConfig.Serialize(pc));
    }
}
