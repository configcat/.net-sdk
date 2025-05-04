using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Override;
using ConfigCat.Client.Tests.Fakes;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ConfigCatClientTests
{
    private readonly Mock<IConfigService> configServiceMock = new();
    private readonly Mock<IConfigCatLogger> loggerMock = new();
    private readonly Mock<IRolloutEvaluator> evaluatorMock = new();
    private readonly Mock<IConfigFetcher> fetcherMock = new();

    [TestInitialize]
    public void TestInitialize()
    {
        this.configServiceMock.Reset();
        this.loggerMock.Reset();
        this.evaluatorMock.Reset();
        this.fetcherMock.Reset();

        this.loggerMock.Setup(l => l.LogLevel).Returns(LogLevel.Warning);
    }

    [ExpectedException(typeof(ArgumentException))]
    [TestMethod]
    [DoNotParallelize]
    public void CreateAnInstance_WhenSdkKeyIsEmpty_ShouldThrowArgumentNullException()
    {
        var sdkKey = string.Empty;

        using var _ = ConfigCatClient.Get(sdkKey);
    }

    [ExpectedException(typeof(ArgumentNullException))]
    [TestMethod]
    [DoNotParallelize]
    public void CreateAnInstance_WhenSdkKeyIsNull_ShouldThrowArgumentNullException()
    {
        string? sdkKey = null;

        using var _ = ConfigCatClient.Get(sdkKey!);
    }

    [DataRow("sdk-key-90123456789012", false, false)]
    [DataRow("sdk-key-9012345678901/1234567890123456789012", false, false)]
    [DataRow("sdk-key-90123456789012/123456789012345678901", false, false)]
    [DataRow("sdk-key-90123456789012/12345678901234567890123", false, false)]
    [DataRow("sdk-key-901234567890123/1234567890123456789012", false, false)]
    [DataRow("sdk-key-90123456789012/1234567890123456789012", false, true)]
    [DataRow("configcat-sdk-1/sdk-key-90123456789012", false, false)]
    [DataRow("configcat-sdk-1/sdk-key-9012345678901/1234567890123456789012", false, false)]
    [DataRow("configcat-sdk-1/sdk-key-90123456789012/123456789012345678901", false, false)]
    [DataRow("configcat-sdk-1/sdk-key-90123456789012/12345678901234567890123", false, false)]
    [DataRow("configcat-sdk-1/sdk-key-901234567890123/1234567890123456789012", false, false)]
    [DataRow("configcat-sdk-1/sdk-key-90123456789012/1234567890123456789012", false, true)]
    [DataRow("configcat-sdk-2/sdk-key-90123456789012/1234567890123456789012", false, false)]
    [DataRow("configcat-proxy/", false, false)]
    [DataRow("configcat-proxy/", true, false)]
    [DataRow("configcat-proxy/sdk-key-90123456789012", false, false)]
    [DataRow("configcat-proxy/sdk-key-90123456789012", true, true)]
    [DataTestMethod]
    [DoNotParallelize]
    public void SdkKeyFormat_ShouldBeValidated(string sdkKey, bool customBaseUrl, bool isValid)
    {
        Action<ConfigCatClientOptions>? configureOptions = customBaseUrl
            ? o => o.BaseUrl = new Uri("https://my-configcat-proxy")
            : null;

        if (isValid)
        {
            using var _ = ConfigCatClient.Get(sdkKey, configureOptions);
        }
        else
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                using var _ = ConfigCatClient.Get(sdkKey, configureOptions);
            });
        }
    }

    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    [TestMethod]
    [DoNotParallelize]
    public void CreateAnInstance_WhenAutoPollConfigurationPollIntervalsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using var _ = ConfigCatClient.Get("hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf", options =>
        {
            options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(0));
        });
    }

    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    [TestMethod]
    [DoNotParallelize]
    public void CreateAnInstance_WhenLazyLoadConfigurationTimeToLiveSecondsIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using var _ = ConfigCatClient.Get("hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf", options =>
        {
            options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(0));
        });
    }

    [TestMethod]
    [DoNotParallelize]
    public void CreateAnInstance_WhenLoggerIsNull_ShouldCreateAnInstance()
    {
        using var client = ConfigCatClient.Get("hsdrTr4sxbHdSgdhHRZds3/GsdrTr4sxbHdSgdhHRZds3", options =>
        {
            options.Logger = null;
        });

        Assert.IsNotNull(client);
    }

    [TestMethod]
    [DoNotParallelize]
    public void CreateAnInstance_WithSdkKey_ShouldCreateAnInstance()
    {
        using var _ = ConfigCatClient.Get("hsdrTr4sxbHdSgdhHRZds3/GsdrTr4sxbHdSgdhHRZds3");
    }

    [TestMethod]
    public async Task Initialization_AutoPoll_ConfigChangedInEveryFetch_ShouldFireConfigChangedEveryPollingIteration()
    {
        var pollInterval = TimeSpan.FromSeconds(1);

        var fetchCounter = 0;
        var configChangedEventCount = 0;

        var hooks = new Hooks();
        hooks.ConfigChanged += delegate { configChangedEventCount++; };

        using var client = CreateClientWithMockedFetcher("1", this.loggerMock, this.fetcherMock,
            onFetch: cfg => FetchResult.Success(ConfigHelper.FromString("{}", httpETag: $"\"{(++fetchCounter).ToString(CultureInfo.InvariantCulture)}\"", timeStamp: ProjectConfig.GenerateTimeStamp())),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, hooks) => new AutoPollConfigService(PollingModes.AutoPoll(pollInterval), this.fetcherMock.Object, cacheParams, loggerWrapper, hooks: hooks),
            hooks, out _, out _
        );

        await Task.Delay(TimeSpan.FromMilliseconds(2.5 * pollInterval.TotalMilliseconds));

        Assert.AreEqual(3, configChangedEventCount);
    }

    [TestMethod]
    public async Task Initialization_AutoPoll_ConfigNotChanged_ShouldFireConfigChangedOnlyOnce()
    {
        var pollInterval = TimeSpan.FromSeconds(1);

        var fetchCounter = 0;
        var configChangedEventCount = 0;

        var hooks = new Hooks();
        hooks.ConfigChanged += delegate { configChangedEventCount++; };

        var pc = ConfigHelper.FromString("{}", httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp());

        using var client = CreateClientWithMockedFetcher("1", this.loggerMock, this.fetcherMock,
            onFetch: cfg => fetchCounter++ > 0
                ? FetchResult.NotModified(pc.With(ProjectConfig.GenerateTimeStamp()))
                : FetchResult.Success(pc.With(ProjectConfig.GenerateTimeStamp())),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, hooks) => new AutoPollConfigService(PollingModes.AutoPoll(pollInterval), this.fetcherMock.Object, cacheParams, loggerWrapper, hooks: hooks),
            hooks, out _, out _
        );

        await Task.Delay(TimeSpan.FromMilliseconds(2.5 * pollInterval.TotalMilliseconds));

        Assert.AreEqual(1, configChangedEventCount);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Initialization_AutoPoll_WithMaxInitWaitTime_GetValueShouldWait(bool isAsync)
    {
        var maxInitWaitTime = TimeSpan.FromSeconds(2);
        var delay = TimeSpan.FromMilliseconds(maxInitWaitTime.TotalMilliseconds / 4);

        var configJsonFilePath = Path.Combine("data", "sample_v5.json");
        var pc = ConfigHelper.FromFile(configJsonFilePath, httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp());

        var sw = new Stopwatch();
        sw.Start();
        using var client = CreateClientWithMockedFetcher("1", this.loggerMock, this.fetcherMock,
            onFetch: _ => throw new NotImplementedException(),
            onFetchAsync: async (cfg, _) =>
            {
                await Task.Delay(delay);
                return FetchResult.Success(pc.With(ProjectConfig.GenerateTimeStamp()));
            },
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, _) => new AutoPollConfigService(PollingModes.AutoPoll(maxInitWaitTime: maxInitWaitTime), this.fetcherMock.Object, cacheParams, loggerWrapper),
            evaluatorFactory: null, configCacheFactory: null, overrideDataSourceFactory: null, hooks: null, out _
        );

        var actualValue = isAsync
            ? await client.GetValueAsync("boolDefaultTrue", false)
            : client.GetValue("boolDefaultTrue", false);

        sw.Stop();

        Assert.IsTrue(sw.Elapsed >= delay - TimeSpan.FromMilliseconds(50), $"Elapsed time: {sw.Elapsed}"); // 50ms for tolerance
        Assert.IsTrue(sw.Elapsed <= delay + TimeSpan.FromMilliseconds(250), $"Elapsed time: {sw.Elapsed}"); // 250ms for tolerance
        Assert.IsTrue(actualValue);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Initialization_AutoPoll_WithMaxInitWaitTime_GetValueShouldWaitForMaxInitWaitTimeOnlyAndReturnDefaultValue(bool isAsync)
    {
        var maxInitWaitTime = TimeSpan.FromSeconds(1);
        var delay = TimeSpan.FromMilliseconds(maxInitWaitTime.TotalMilliseconds * 4);

        var configJsonFilePath = Path.Combine("data", "sample_v5.json");
        var pc = ConfigHelper.FromFile(configJsonFilePath, httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp());

        var sw = new Stopwatch();
        sw.Start();
        using var client = CreateClientWithMockedFetcher("1", this.loggerMock, this.fetcherMock,
            onFetch: _ => throw new NotImplementedException(),
            onFetchAsync: async (cfg, _) =>
            {
                await Task.Delay(delay);
                return FetchResult.Success(pc.With(ProjectConfig.GenerateTimeStamp()));
            },
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, _) => new AutoPollConfigService(PollingModes.AutoPoll(maxInitWaitTime: maxInitWaitTime), this.fetcherMock.Object, cacheParams, loggerWrapper),
            evaluatorFactory: null, configCacheFactory: null, overrideDataSourceFactory: null, hooks: null, out _
        );

        var actualValue = isAsync
            ? await client.GetValueAsync("boolDefaultTrue", false)
            : client.GetValue("boolDefaultTrue", false);

        sw.Stop();

        Assert.IsTrue(sw.Elapsed >= maxInitWaitTime - TimeSpan.FromMilliseconds(50), $"Elapsed time: {sw.Elapsed}"); // 50ms for tolerance
        Assert.IsTrue(sw.Elapsed <= maxInitWaitTime + TimeSpan.FromMilliseconds(250), $"Elapsed time: {sw.Elapsed}"); // 250ms for tolerance

        Assert.IsFalse(actualValue);
    }

    [TestMethod]
    public async Task WaitForReadyAsync_AutoPoll_ShouldWait()
    {
        var configJsonFilePath = Path.Combine("data", "sample_v5.json");
        var pc = ConfigHelper.FromFile(configJsonFilePath, httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp());

        var hooks = new Hooks();

        using var client = CreateClientWithMockedFetcher("1", this.loggerMock, this.fetcherMock,
            onFetch: cfg => FetchResult.Success(pc.With(ProjectConfig.GenerateTimeStamp())),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, hooks) => new AutoPollConfigService(PollingModes.AutoPoll(), this.fetcherMock.Object, cacheParams, loggerWrapper, hooks: hooks),
            hooks, out _, out _
        );

        var cacheState = await client.WaitForReadyAsync();

        Assert.AreEqual(ClientCacheState.HasUpToDateFlagData, cacheState);

        var snapshot = client.Snapshot();
        Assert.IsTrue(snapshot.GetValue("boolDefaultTrue", false));

        var evaluationDetails = snapshot.GetValueDetails("boolDefaultTrue", false);
        Assert.IsFalse(evaluationDetails.IsDefaultValue);
        Assert.IsTrue(evaluationDetails.Value);
    }

    [TestMethod]
    public async Task WaitForReadyAsync_AutoPoll_ShouldWaitForMaxInitWaitTime()
    {
        var maxInitWaitTime = TimeSpan.FromSeconds(1);
        var delay = TimeSpan.FromMilliseconds(maxInitWaitTime.TotalMilliseconds * 4);

        var configJsonFilePath = Path.Combine("data", "sample_v5.json");
        var pc = ConfigHelper.FromFile(configJsonFilePath, httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp());

        var hooks = new Hooks();

        var sw = new Stopwatch();
        sw.Start();
        using var client = CreateClientWithMockedFetcher("1", this.loggerMock, this.fetcherMock,
            onFetch: _ => throw new NotImplementedException(),
            onFetchAsync: async (cfg, _) =>
            {
                await Task.Delay(delay);
                return FetchResult.Success(pc.With(ProjectConfig.GenerateTimeStamp()));
            },
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, hooks) => new AutoPollConfigService(PollingModes.AutoPoll(maxInitWaitTime: maxInitWaitTime), this.fetcherMock.Object, cacheParams, loggerWrapper, hooks: hooks),
            evaluatorFactory: null, configCacheFactory: null, overrideDataSourceFactory: null, hooks, out _
        );

        var cacheState = await client.WaitForReadyAsync();

        sw.Stop();

        Assert.IsTrue(sw.Elapsed >= maxInitWaitTime - TimeSpan.FromMilliseconds(50), $"Elapsed time: {sw.Elapsed}"); // 50ms for tolerance
        Assert.IsTrue(sw.Elapsed <= maxInitWaitTime + TimeSpan.FromMilliseconds(250), $"Elapsed time: {sw.Elapsed}"); // 250ms for tolerance

        Assert.AreEqual(ClientCacheState.NoFlagData, cacheState);

        var snapshot = client.Snapshot();
        Assert.IsFalse(snapshot.GetValue("boolDefaultTrue", false));

        var evaluationDetails = snapshot.GetValueDetails("boolDefaultTrue", false);
        Assert.IsTrue(evaluationDetails.IsDefaultValue);
        Assert.IsFalse(evaluationDetails.Value);
    }

    [TestMethod]
    public async Task WaitForReadyAsync_AutoPoll_ShouldWaitForMaxInitWaitTimeAndReturnCached()
    {
        var maxInitWaitTime = TimeSpan.FromSeconds(1);
        var pollInterval = TimeSpan.FromSeconds(5);
        var delay = TimeSpan.FromMilliseconds(maxInitWaitTime.TotalMilliseconds * 4);

        var configJsonFilePath = Path.Combine("data", "sample_v5.json");
        var pc = ConfigHelper.FromFile(configJsonFilePath, httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp() - pollInterval - TimeSpan.FromSeconds(1));

        const string cacheKey = "1";
        var externalCache = new FakeExternalCache();
        externalCache.Set(cacheKey, ProjectConfig.Serialize(pc));

        var hooks = new Hooks();

        var sw = new Stopwatch();
        sw.Start();
        using var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => throw new NotImplementedException(),
            onFetchAsync: async (cfg, _) =>
            {
                await Task.Delay(delay);
                return FetchResult.NotModified(pc.With(ProjectConfig.GenerateTimeStamp()));
            },
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, hooks) => new AutoPollConfigService(PollingModes.AutoPoll(pollInterval, maxInitWaitTime), this.fetcherMock.Object, cacheParams, loggerWrapper, hooks: hooks),
            evaluatorFactory: null,
            configCacheFactory: logger => new ExternalConfigCache(externalCache, logger),
            overrideDataSourceFactory: null, hooks, out _
        );

        var cacheState = await client.WaitForReadyAsync();

        sw.Stop();

        Assert.IsTrue(sw.Elapsed >= maxInitWaitTime - TimeSpan.FromMilliseconds(50), $"Elapsed time: {sw.Elapsed}"); // 50ms for tolerance
        Assert.IsTrue(sw.Elapsed <= maxInitWaitTime + TimeSpan.FromMilliseconds(250), $"Elapsed time: {sw.Elapsed}"); // 250ms for tolerance

        Assert.AreEqual(ClientCacheState.HasCachedFlagDataOnly, cacheState);

        var snapshot = client.Snapshot();
        Assert.IsTrue(snapshot.GetValue("boolDefaultTrue", false));

        var evaluationDetails = snapshot.GetValueDetails("boolDefaultTrue", false);
        Assert.IsFalse(evaluationDetails.IsDefaultValue);
        Assert.IsTrue(evaluationDetails.Value);
    }

    [TestMethod]
    public async Task WaitForReadyAsync_LazyLoad_ReturnCached_UpToDate()
    {
        var cacheTimeToLive = TimeSpan.FromSeconds(2);

        var configJsonFilePath = Path.Combine("data", "sample_v5.json");
        var pc = ConfigHelper.FromFile(configJsonFilePath, httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp());

        const string cacheKey = "1";
        var externalCache = new FakeExternalCache();
        externalCache.Set(cacheKey, ProjectConfig.Serialize(pc));

        var hooks = new Hooks();

        using var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: cfg => FetchResult.NotModified(pc.With(ProjectConfig.GenerateTimeStamp())),
            onFetchAsync: (cfg, _) => Task.FromResult(FetchResult.NotModified(pc.With(ProjectConfig.GenerateTimeStamp()))),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, hooks) => new LazyLoadConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper, cacheTimeToLive, hooks: hooks),
            evaluatorFactory: null,
            configCacheFactory: logger => new ExternalConfigCache(externalCache, logger),
            overrideDataSourceFactory: null, hooks, out _
        );

        var cacheState = await client.WaitForReadyAsync();

        Assert.AreEqual(ClientCacheState.HasUpToDateFlagData, cacheState);

        var snapshot = client.Snapshot();
        Assert.IsTrue(snapshot.GetValue("boolDefaultTrue", false));

        var evaluationDetails = snapshot.GetValueDetails("boolDefaultTrue", false);
        Assert.IsFalse(evaluationDetails.IsDefaultValue);
        Assert.IsTrue(evaluationDetails.Value);
    }

    [TestMethod]
    public async Task WaitForReadyAsync_LazyLoad_ReturnCached_Expired()
    {
        var cacheTimeToLive = TimeSpan.FromSeconds(2);

        var configJsonFilePath = Path.Combine("data", "sample_v5.json");
        var pc = ConfigHelper.FromFile(configJsonFilePath, httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp() - cacheTimeToLive - TimeSpan.FromSeconds(1));

        const string cacheKey = "1";
        var externalCache = new FakeExternalCache();
        externalCache.Set(cacheKey, ProjectConfig.Serialize(pc));

        var hooks = new Hooks();

        using var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: cfg => FetchResult.NotModified(pc.With(ProjectConfig.GenerateTimeStamp())),
            onFetchAsync: (cfg, _) => Task.FromResult(FetchResult.NotModified(pc.With(ProjectConfig.GenerateTimeStamp()))),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, hooks) => new LazyLoadConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper, cacheTimeToLive, hooks: hooks),
            evaluatorFactory: null,
            configCacheFactory: logger => new ExternalConfigCache(externalCache, logger),
            overrideDataSourceFactory: null, hooks, out _
        );

        var cacheState = await client.WaitForReadyAsync();

        Assert.AreEqual(ClientCacheState.HasCachedFlagDataOnly, cacheState);

        var snapshot = client.Snapshot();
        Assert.IsTrue(snapshot.GetValue("boolDefaultTrue", false));

        var evaluationDetails = snapshot.GetValueDetails("boolDefaultTrue", false);
        Assert.IsFalse(evaluationDetails.IsDefaultValue);
        Assert.IsTrue(evaluationDetails.Value);
    }

    [TestMethod]
    public async Task WaitForReadyAsync_ManualPoll_ReturnCached()
    {
        var configJsonFilePath = Path.Combine("data", "sample_v5.json");
        var pc = ConfigHelper.FromFile(configJsonFilePath, httpETag: $"\0\"", timeStamp: ProjectConfig.GenerateTimeStamp());

        const string cacheKey = "1";
        var externalCache = new FakeExternalCache();
        externalCache.Set(cacheKey, ProjectConfig.Serialize(pc));

        var hooks = new Hooks();

        using var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: cfg => FetchResult.NotModified(pc.With(ProjectConfig.GenerateTimeStamp())),
            onFetchAsync: (cfg, _) => Task.FromResult(FetchResult.NotModified(pc.With(ProjectConfig.GenerateTimeStamp()))),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, hooks) => new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper, hooks: hooks),
            evaluatorFactory: null,
            configCacheFactory: logger => new ExternalConfigCache(externalCache, logger),
            overrideDataSourceFactory: null, hooks, out _
        );

        var cacheState = await client.WaitForReadyAsync();

        Assert.AreEqual(ClientCacheState.HasCachedFlagDataOnly, cacheState);

        var snapshot = client.Snapshot();
        Assert.IsTrue(snapshot.GetValue("boolDefaultTrue", false));

        var evaluationDetails = snapshot.GetValueDetails("boolDefaultTrue", false);
        Assert.IsFalse(evaluationDetails.IsDefaultValue);
        Assert.IsTrue(evaluationDetails.Value);
    }

    [TestMethod]
    public async Task WaitForReadyAsync_LocalOnlyFlagOverride()
    {
        var configJsonFilePath = Path.Combine("data", "sample_v5.json");

        var hooks = new Hooks();

        using var client = CreateClientWithMockedFetcher("1", this.loggerMock, this.fetcherMock,
            onFetch: delegate { throw new InvalidOperationException(); },
            onFetchAsync: delegate { throw new InvalidOperationException(); },
            configServiceFactory: (_, _, loggerWrapper, hooks) => new NullConfigService(loggerWrapper, hooks: hooks),
            evaluatorFactory: null, configCacheFactory: null,
            overrideDataSourceFactory: logger => Tuple.Create(OverrideBehaviour.LocalOnly, (IOverrideDataSource)new LocalFileDataSource(configJsonFilePath, autoReload: false, logger)),
            hooks, out _
        );

        var cacheState = await client.WaitForReadyAsync();

        Assert.AreEqual(ClientCacheState.HasLocalOverrideFlagDataOnly, cacheState);

        var snapshot = client.Snapshot();
        Assert.IsTrue(snapshot.GetValue("boolDefaultTrue", false));

        var evaluationDetails = snapshot.GetValueDetails("boolDefaultTrue", false);
        Assert.IsFalse(evaluationDetails.IsDefaultValue);
        Assert.IsTrue(evaluationDetails.Value);
    }

    [TestMethod]
    public void GetValue_ConfigServiceThrowException_ShouldReturnDefaultValue()
    {
        // Arrange

        const string defaultValue = "Victory for the Firstborn!";

        this.configServiceMock
            .Setup(m => m.GetConfig())
            .Throws<Exception>();

        var client = new ConfigCatClient(this.configServiceMock.Object, this.loggerMock.Object, this.evaluatorMock.Object);

        // Act

        var actual = client.GetValue("x", defaultValue);

        // Assert

        Assert.AreEqual(defaultValue, actual);
    }

    [TestMethod]
    public async Task GetValueAsync_ConfigServiceThrowException_ShouldReturnDefaultValue()
    {
        // Arrange

        const string defaultValue = "Victory for the Firstborn!";

        this.configServiceMock
            .Setup(m => m.GetConfigAsync(It.IsAny<CancellationToken>()))
            .Throws<Exception>();

        var client = new ConfigCatClient(this.configServiceMock.Object, this.loggerMock.Object, this.evaluatorMock.Object);

        // Act

        var actual = await client.GetValueAsync("x", defaultValue);

        // Assert

        Assert.AreEqual(defaultValue, actual);
    }

    [TestMethod]
    public void GetValue_EvaluateServiceThrowException_ShouldReturnDefaultValue()
    {
        // Arrange

        const string defaultValue = "Victory for the Firstborn!";

        this.configServiceMock
            .Setup(m => m.GetConfig())
            .Throws<Exception>();

        this.evaluatorMock
            .Setup(m => m.Evaluate(It.IsAny<It.IsAnyType>(), ref It.Ref<EvaluateContext>.IsAny, out It.Ref<It.IsAnyType>.IsAny))
            .Throws<Exception>();

        var client = new ConfigCatClient(this.configServiceMock.Object, this.loggerMock.Object, this.evaluatorMock.Object, hooks: new Hooks());

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        // Act

        var actual = client.GetValue("x", defaultValue);

        // Assert

        Assert.AreEqual(defaultValue, actual);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreEqual(defaultValue, flagEvaluatedEvents[0].EvaluationDetails.Value);
        Assert.IsTrue(flagEvaluatedEvents[0].EvaluationDetails.IsDefaultValue);
    }

    [TestMethod]
    public async Task GetValueAsync_EvaluateServiceThrowException_ShouldReturnDefaultValue()
    {
        // Arrange

        const string defaultValue = "Victory for the Firstborn!";

        this.configServiceMock
            .Setup(m => m.GetConfigAsync(It.IsAny<CancellationToken>()))
            .Throws<Exception>();

        this.evaluatorMock
            .Setup(m => m.Evaluate(It.IsAny<It.IsAnyType>(), ref It.Ref<EvaluateContext>.IsAny, out It.Ref<It.IsAnyType>.IsAny))
            .Throws<Exception>();

        var client = new ConfigCatClient(this.configServiceMock.Object, this.loggerMock.Object, this.evaluatorMock.Object, hooks: new Hooks());

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        // Act

        var actual = await client.GetValueAsync("x", defaultValue);

        // Assert

        Assert.AreEqual(defaultValue, actual);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreEqual(defaultValue, flagEvaluatedEvents[0].EvaluationDetails.Value);
        Assert.IsTrue(flagEvaluatedEvents[0].EvaluationDetails.IsDefaultValue);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_ConfigJsonIsNotAvailable(bool isAsync)
    {
        // Arrange

        const string key = "boolean";
        const bool defaultValue = false;

        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => FetchResult.Success(ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", ProjectConfig.GenerateTimeStamp())),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper);
            },
            out var configService, out _);

        var user = new User("a@configcat.com") { Email = "a@configcat.com" };

        // Act

        var actual = isAsync
            ? await client.GetValueDetailsAsync(key, defaultValue, user)
            : client.GetValueDetails(key, defaultValue, user);

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(key, actual.Key);
        Assert.AreEqual(defaultValue, actual.Value);
        Assert.IsTrue(actual.IsDefaultValue);
        Assert.IsNull(actual.VariationId);
        Assert.AreEqual(DateTime.MinValue, actual.FetchTime);
        Assert.AreSame(user, actual.User);
        Assert.AreEqual(EvaluationErrorCode.ConfigJsonNotAvailable, actual.ErrorCode);
        Assert.IsNotNull(actual.ErrorMessage);
        Assert.IsNull(actual.ErrorException);
        Assert.IsNull(actual.MatchedTargetingRule);
        Assert.IsNull(actual.MatchedPercentageOption);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_SettingIsMissing(bool isAsync)
    {
        // Arrange

        const string key = "does-not-exist";
        const bool defaultValue = false;

        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
        var timeStamp = ProjectConfig.GenerateTimeStamp();

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => FetchResult.Success(ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", timeStamp)),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper);
            },
            out var configService, out _);

        if (isAsync)
        {
            await client.ForceRefreshAsync();
        }
        else
        {
            client.ForceRefresh();
        }

        var user = new User("a@configcat.com") { Email = "a@configcat.com" };

        // Act

        var actual = isAsync
            ? await client.GetValueDetailsAsync(key, defaultValue, user)
            : client.GetValueDetails(key, defaultValue, user);

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(key, actual.Key);
        Assert.AreEqual(defaultValue, actual.Value);
        Assert.IsTrue(actual.IsDefaultValue);
        Assert.IsNull(actual.VariationId);
        Assert.AreEqual(timeStamp, actual.FetchTime);
        Assert.AreSame(user, actual.User);
        Assert.AreEqual(EvaluationErrorCode.SettingKeyMissing, actual.ErrorCode);
        Assert.IsNotNull(actual.ErrorMessage);
        Assert.IsNull(actual.ErrorException);
        Assert.IsNull(actual.MatchedTargetingRule);
        Assert.IsNull(actual.MatchedPercentageOption);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_SettingIsAvailableButNoRulesApply(bool isAsync)
    {
        // Arrange

        const string key = "boolean";
        const bool defaultValue = false;

        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
        var timeStamp = ProjectConfig.GenerateTimeStamp();

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => FetchResult.Success(ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", timeStamp)),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper);
            },
            out var configService, out _);

        if (isAsync)
        {
            await client.ForceRefreshAsync();
        }
        else
        {
            client.ForceRefresh();
        }

        // Act

        var actual = isAsync
            ? await client.GetValueDetailsAsync(key, defaultValue, user: null)
            : client.GetValueDetails(key, defaultValue, user: null);

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(key, actual.Key);
        Assert.AreEqual(false, actual.Value);
        Assert.IsFalse(actual.IsDefaultValue);
        Assert.AreEqual("a0e56eda", actual.VariationId);
        Assert.AreEqual(timeStamp, actual.FetchTime);
        Assert.IsNull(actual.User);
        Assert.AreEqual(EvaluationErrorCode.None, actual.ErrorCode);
        Assert.IsNull(actual.ErrorMessage);
        Assert.IsNull(actual.ErrorException);
        Assert.IsNull(actual.MatchedTargetingRule);
        Assert.IsNull(actual.MatchedPercentageOption);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_SettingIsAvailableAndComparisonRuleApplies(bool isAsync)
    {
        // Arrange

        const string key = "boolean";
        const bool defaultValue = false;

        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
        var timeStamp = ProjectConfig.GenerateTimeStamp();

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => FetchResult.Success(ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", timeStamp)),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper);
            },
            out var configService, out _);

        if (isAsync)
        {
            await client.ForceRefreshAsync();
        }
        else
        {
            client.ForceRefresh();
        }

        var user = new User("a@configcat.com") { Email = "a@configcat.com" };

        // Act

        var actual = isAsync
            ? await client.GetValueDetailsAsync(key, defaultValue, user)
            : client.GetValueDetails(key, defaultValue, user);

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(key, actual.Key);
        Assert.AreEqual(true, actual.Value);
        Assert.IsFalse(actual.IsDefaultValue);
        Assert.AreEqual("67787ae4", actual.VariationId);
        Assert.AreEqual(timeStamp, actual.FetchTime);
        Assert.AreSame(user, actual.User);
        Assert.AreEqual(EvaluationErrorCode.None, actual.ErrorCode);
        Assert.IsNull(actual.ErrorMessage);
        Assert.IsNull(actual.ErrorException);
        Assert.IsNotNull(actual.MatchedTargetingRule);
        Assert.IsNull(actual.MatchedPercentageOption);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_SettingIsAvailableAndPercentageRuleApplies(bool isAsync)
    {
        // Arrange

        const string key = "boolean";
        const bool defaultValue = false;

        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
        var timeStamp = ProjectConfig.GenerateTimeStamp();

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => FetchResult.Success(ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", timeStamp)),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper);
            },
            out var configService, out _);

        if (isAsync)
        {
            await client.ForceRefreshAsync();
        }
        else
        {
            client.ForceRefresh();
        }

        var user = new User("a@example.com") { Email = "a@example.com" };

        // Act

        var actual = isAsync
            ? await client.GetValueDetailsAsync(key, defaultValue, user)
            : client.GetValueDetails(key, defaultValue, user);

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(key, actual.Key);
        Assert.AreEqual(true, actual.Value);
        Assert.IsFalse(actual.IsDefaultValue);
        Assert.AreEqual("67787ae4", actual.VariationId);
        Assert.AreEqual(timeStamp, actual.FetchTime);
        Assert.AreSame(user, actual.User);
        Assert.AreEqual(EvaluationErrorCode.None, actual.ErrorCode);
        Assert.IsNull(actual.ErrorMessage);
        Assert.IsNull(actual.ErrorException);
        Assert.IsNull(actual.MatchedTargetingRule);
        Assert.IsNotNull(actual.MatchedPercentageOption);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetValueDetails_ConfigServiceThrowException_ShouldReturnDefaultValue(bool isAsync)
    {
        // Arrange

        const string key = "Feature";
        const string defaultValue = "Victory for the Firstborn!";
        const string errorMessage = "Error";

        if (isAsync)
        {
            this.configServiceMock.Setup(m => m.GetConfigAsync(It.IsAny<CancellationToken>())).Throws(new ApplicationException(errorMessage));
        }
        else
        {
            this.configServiceMock.Setup(m => m.GetConfig()).Throws(new ApplicationException(errorMessage));
        }

        var client = new ConfigCatClient(this.configServiceMock.Object, this.loggerMock.Object, this.evaluatorMock.Object);

        // Act

        var actual = isAsync
            ? await client.GetValueDetailsAsync(key, defaultValue)
            : client.GetValueDetails(key, defaultValue);

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(key, actual!.Key);
        Assert.AreEqual(defaultValue, actual.Value);
        Assert.IsTrue(actual.IsDefaultValue);
        Assert.IsNull(actual.VariationId);
        Assert.AreEqual(DateTime.MinValue, actual.FetchTime);
        Assert.IsNull(actual.User);
        Assert.AreEqual(EvaluationErrorCode.UnexpectedError, actual.ErrorCode);
        Assert.AreEqual(errorMessage, actual.ErrorMessage);
        Assert.IsInstanceOfType(actual.ErrorException, typeof(ApplicationException));
        Assert.IsNull(actual.MatchedTargetingRule);
        Assert.IsNull(actual.MatchedPercentageOption);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetValueDetails_EvaluateServiceThrowException_ShouldReturnDefaultValue(bool isAsync)
    {
        // Arrange

        const string key = "boolean";
        const string defaultValue = "Victory for the Firstborn!";
        const string errorMessage = "Error";

        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
        var timeStamp = ProjectConfig.GenerateTimeStamp();

        this.evaluatorMock
            .Setup(m => m.Evaluate(It.IsAny<It.IsAnyType>(), ref It.Ref<EvaluateContext>.IsAny, out It.Ref<It.IsAnyType>.IsAny))
            .Throws(new ApplicationException(errorMessage));

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => FetchResult.Success(ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", timeStamp)),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, _) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper);
            },
            evaluatorFactory: _ => this.evaluatorMock.Object, new Hooks(),
            out var configService, out _);

        if (isAsync)
        {
            await client.ForceRefreshAsync();
        }
        else
        {
            client.ForceRefresh();
        }

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var user = new User("a@example.com") { Email = "a@example.com" };

        // Act

        var actual = isAsync
            ? await client.GetValueDetailsAsync(key, defaultValue, user)
            : client.GetValueDetails(key, defaultValue, user);

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(key, actual!.Key);
        Assert.AreEqual(defaultValue, actual.Value);
        Assert.IsTrue(actual.IsDefaultValue);
        Assert.IsNull(actual.VariationId);
        Assert.AreEqual(timeStamp, actual.FetchTime);
        Assert.AreSame(user, actual.User);
        Assert.AreEqual(EvaluationErrorCode.UnexpectedError, actual.ErrorCode);
        Assert.AreEqual(errorMessage, actual.ErrorMessage);
        Assert.IsInstanceOfType(actual.ErrorException, typeof(ApplicationException));
        Assert.IsNull(actual.MatchedTargetingRule);
        Assert.IsNull(actual.MatchedPercentageOption);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreSame(actual, flagEvaluatedEvents[0].EvaluationDetails);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetAllValueDetails_ShouldReturnCorrectEvaluationDetails(bool isAsync)
    {
        // Arrange

        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
        var timeStamp = ProjectConfig.GenerateTimeStamp();

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => FetchResult.Success(ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", timeStamp)),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, _) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper);
            },
            evaluatorFactory: null, new Hooks(), out var configService, out _);

        if (isAsync)
        {
            await client.ForceRefreshAsync();
        }
        else
        {
            client.ForceRefresh();
        }

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var user = new User("a@configcat.com") { Email = "a@configcat.com" };

        // Act

        var actual = isAsync
            ? await client.GetAllValueDetailsAsync(user)
            : client.GetAllValueDetails(user);

        // Assert

        var expected = new[]
        {
            new { Key = "boolean", Value = (object)true, VariationId = "67787ae4" },
            new { Key = "text", Value = (object)"true", VariationId = "9bdc6a1f" },
            new { Key = "whole", Value = (object)1, VariationId = "ab30533b" },
            new { Key = "decimal", Value = (object)-2147483647.2147484, VariationId = "8f9559cf" },
        };

        foreach (var expectedItem in expected)
        {
            var actualDetails = actual.FirstOrDefault(details => details.Key == expectedItem.Key);

            Assert.IsNotNull(actualDetails);
            Assert.AreEqual(expectedItem.Value, actualDetails.Value);
            Assert.IsFalse(actualDetails.IsDefaultValue);
            Assert.AreEqual(expectedItem.VariationId, actualDetails.VariationId);
            Assert.AreEqual(timeStamp, actualDetails.FetchTime);
            Assert.AreSame(user, actualDetails.User);
            Assert.AreEqual(EvaluationErrorCode.None, actualDetails.ErrorCode);
            Assert.IsNull(actualDetails.ErrorMessage);
            Assert.IsNull(actualDetails.ErrorException);
            Assert.IsNotNull(actualDetails.MatchedTargetingRule);
            Assert.IsNull(actualDetails.MatchedPercentageOption);

            var flagEvaluatedDetails = flagEvaluatedEvents.Select(e => e.EvaluationDetails).FirstOrDefault(details => details.Key == expectedItem.Key);

            Assert.IsNotNull(flagEvaluatedDetails);
            Assert.AreSame(actualDetails, flagEvaluatedDetails);
        }
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetAllValueDetails_DeserializeFailed_ShouldReturnWithEmptyArray(bool isAsync)
    {
        // Arrange

        this.configServiceMock.Setup(m => m.GetConfig()).Returns(ProjectConfig.Empty);
        this.configServiceMock.Setup(m => m.GetConfigAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ProjectConfig.Empty);
        var o = new Config();

        using IConfigCatClient client = new ConfigCatClient(this.configServiceMock.Object, this.loggerMock.Object, this.evaluatorMock.Object, hooks: new Hooks());

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        // Act

        var actual = isAsync
            ? await client.GetAllValueDetailsAsync()
            : client.GetAllValueDetails();

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(0, actual.Count);
        Assert.AreEqual(0, flagEvaluatedEvents.Count);
        this.loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetAllValueDetails_ConfigServiceThrowException_ShouldReturnEmptyEnumerable(bool isAsync)
    {
        // Arrange

        this.configServiceMock
            .Setup(m => m.GetConfig())
            .Throws<Exception>();

        this.configServiceMock
            .Setup(m => m.GetConfigAsync(It.IsAny<CancellationToken>()))
            .Throws<Exception>();

        using IConfigCatClient client = new ConfigCatClient(this.configServiceMock.Object, this.loggerMock.Object, this.evaluatorMock.Object, hooks: new Hooks());

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        // Act

        var actual = isAsync
            ? await client.GetAllValueDetailsAsync()
            : client.GetAllValueDetails();

        // Assert

        Assert.IsNotNull(actual);
        Assert.AreEqual(0, actual.Count);
        Assert.AreEqual(0, flagEvaluatedEvents.Count);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task GetAllValueDetails_EvaluateServiceThrowException_ShouldReturnDefaultValue(bool isAsync)
    {
        // Arrange

        const string errorMessage = "Error";

        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
        var timeStamp = ProjectConfig.GenerateTimeStamp();

        this.evaluatorMock
            .Setup(m => m.Evaluate(It.IsAny<It.IsAnyType>(), ref It.Ref<EvaluateContext>.IsAny, out It.Ref<It.IsAnyType>.IsAny))
            .Throws(new ApplicationException(errorMessage));

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: _ => FetchResult.Success(ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", timeStamp)),
            configServiceFactory: (fetcher, cacheParams, loggerWrapper, _) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper);
            },
            evaluatorFactory: _ => this.evaluatorMock.Object, new Hooks(),
            out var configService, out _);

        if (isAsync)
        {
            await client.ForceRefreshAsync();
        }
        else
        {
            client.ForceRefresh();
        }

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        var errorEvents = new List<ConfigCatClientErrorEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);
        client.Error += (s, e) => errorEvents.Add(e);

        var user = new User("a@example.com") { Email = "a@example.com" };

        // Act

        var actual = isAsync
            ? await client.GetAllValueDetailsAsync(user)
            : client.GetAllValueDetails(user);

        // Assert

        foreach (var key in new[] { "boolean", "text", "whole", "decimal" })
        {
            var actualDetails = actual.FirstOrDefault(details => details.Key == key);

            Assert.IsNotNull(actualDetails);
            Assert.AreEqual(key, actualDetails!.Key);
            Assert.IsNull(actualDetails.Value);
            Assert.IsTrue(actualDetails.IsDefaultValue);
            Assert.IsNull(actualDetails.VariationId);
            Assert.AreEqual(timeStamp, actualDetails.FetchTime);
            Assert.AreSame(user, actualDetails.User);
            Assert.AreEqual(EvaluationErrorCode.UnexpectedError, actualDetails.ErrorCode);
            Assert.AreEqual(errorMessage, actualDetails.ErrorMessage);
            Assert.IsInstanceOfType(actualDetails.ErrorException, typeof(ApplicationException));
            Assert.IsNull(actualDetails.MatchedTargetingRule);
            Assert.IsNull(actualDetails.MatchedPercentageOption);

            var flagEvaluatedDetails = flagEvaluatedEvents.Select(e => e.EvaluationDetails).FirstOrDefault(details => details.Key == key);

            Assert.IsNotNull(flagEvaluatedDetails);
            Assert.AreSame(actualDetails, flagEvaluatedDetails);
        }

        Assert.AreEqual(1, errorEvents.Count);
        var errorEventArgs = errorEvents[0];
        StringAssert.Contains(errorEventArgs.Message, isAsync ? nameof(IConfigCatClient.GetAllValueDetailsAsync) : nameof(IConfigCatClient.GetAllValueDetails));
        Assert.IsInstanceOfType(errorEventArgs.Exception, typeof(AggregateException));
        var actualException = (AggregateException)errorEventArgs.Exception!;
        Assert.AreEqual(actual.Count, actualException.InnerExceptions.Count);
        foreach (var ex in actualException.InnerExceptions)
        {
            Assert.IsInstanceOfType(ex, typeof(ApplicationException));
        }
    }

    [TestMethod]
    public async Task GetAllKeys_ConfigServiceThrowException_ShouldReturnsWithEmptyArray()
    {
        // Arrange

        this.configServiceMock.Setup(m => m.GetConfigAsync(It.IsAny<CancellationToken>())).Throws<Exception>();

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var actualKeys = await instance.GetAllKeysAsync();

        // Assert

        Assert.IsNotNull(actualKeys);
        Assert.AreEqual(0, actualKeys.Count());
        this.loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public void GetAllKeys_ConfigServiceThrowException_ShouldReturnsWithEmptyArray_Sync()
    {
        // Arrange

        this.configServiceMock.Setup(m => m.GetConfig()).Throws<Exception>();

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var actualKeys = instance.GetAllKeys();

        // Assert

        Assert.IsNotNull(actualKeys);
        Assert.AreEqual(0, actualKeys.Count());
        this.loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public void GetAllKeys_DeserializerThrowException_ShouldReturnsWithEmptyArray()
    {
        // Arrange

        this.configServiceMock.Setup(m => m.GetConfig()).Returns(ProjectConfig.Empty);
        var o = new Config();

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var actualKeys = instance.GetAllKeys();

        // Assert

        Assert.IsNotNull(actualKeys);
        Assert.AreEqual(0, actualKeys.Count());
        this.loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public async Task GetAllKeysAsync_DeserializeFailed_ShouldReturnsWithEmptyArray()
    {
        // Arrange

        this.configServiceMock.Setup(m => m.GetConfigAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ProjectConfig.Empty);

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var actualKeys = await instance.GetAllKeysAsync();

        // Assert

        Assert.IsNotNull(actualKeys);
        Assert.AreEqual(0, actualKeys.Count());
        this.loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public void GetAllKeys_DeserializeFailed_ShouldReturnsWithEmptyArray()
    {
        // Arrange

        this.configServiceMock.Setup(m => m.GetConfig()).Returns(ProjectConfig.Empty);

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var actualKeys = instance.GetAllKeys();

        // Assert

        Assert.IsNotNull(actualKeys);
        Assert.AreEqual(0, actualKeys.Count());
        this.loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
    }

    [DataRow(nameof(IConfigCatClient.GetValueAsync))]
    [DataRow(nameof(IConfigCatClient.GetValueDetailsAsync))]
    [DataRow(nameof(IConfigCatClient.GetAllKeysAsync))]
    [DataRow(nameof(IConfigCatClient.GetAllValuesAsync))]
    [DataRow(nameof(IConfigCatClient.GetAllValueDetailsAsync))]
    [DataTestMethod]
    public async Task EvaluationMethods_ShouldBeCancelable(string methodName)
    {
        // Arrange

        const int delayMs = 2000;

        var loggerWrapper = this.loggerMock.Object.AsWrapper();
        var fakeHandler = new FakeHttpClientHandler(HttpStatusCode.OK, "{ }", TimeSpan.FromMilliseconds(delayMs));
        var configFetcher = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", loggerWrapper, new HttpClientConfigFetcher(fakeHandler), false, TimeSpan.FromMilliseconds(delayMs * 2));
        var configCache = new InMemoryConfigCache();
        var cacheParams = new CacheParameters(configCache, cacheKey: null!);
        var configService = new LazyLoadConfigService(configFetcher, cacheParams, loggerWrapper, TimeSpan.FromSeconds(1));
        var evaluator = new RolloutEvaluator(loggerWrapper);
        using var client = new ConfigCatClient(configService, loggerWrapper, evaluator);

        // Act

        using var cts = new CancellationTokenSource(delayMs / 4);

        Func<Task> action = methodName switch
        {
            nameof(IConfigCatClient.GetValueAsync) => () => client.GetValueAsync("KEY", "", cancellationToken: cts.Token),
            nameof(IConfigCatClient.GetValueDetailsAsync) => () => client.GetValueDetailsAsync("KEY", "", cancellationToken: cts.Token),
            nameof(IConfigCatClient.GetAllKeysAsync) => () => client.GetAllKeysAsync(cancellationToken: cts.Token),
            nameof(IConfigCatClient.GetAllValuesAsync) => () => client.GetAllValuesAsync(cancellationToken: cts.Token),
            nameof(IConfigCatClient.GetAllValueDetailsAsync) => () => client.GetAllValueDetailsAsync(cancellationToken: cts.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName))
        };

        // Assert

        var ex = await Assert.ThrowsExceptionAsync<TaskCanceledException>(action);
        Assert.AreEqual(cts.Token, ex.CancellationToken);
    }

    [TestMethod]
    public void Dispose_ConfigServiceIsDisposable_ShouldInvokeDispose()
    {
        // Arrange

        var myMock = new FakeConfigService(Mock.Of<IConfigFetcher>(), new CacheParameters(), Mock.Of<IConfigCatLogger>().AsWrapper());

        IConfigCatClient instance = new ConfigCatClient(
            myMock,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        instance.Dispose();

        // Assert

        Assert.AreEqual(1, myMock.DisposeCount);
    }

    [TestMethod]
    public async Task ForceRefresh_ShouldInvokeConfigServiceRefreshConfigAsync()
    {
        // Arrange

        this.configServiceMock.Setup(m => m.RefreshConfigAsync(It.IsAny<CancellationToken>())).ReturnsAsync(RefreshResult.Success());

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var result = await instance.ForceRefreshAsync();

        // Assert

        this.configServiceMock.Verify(m => m.RefreshConfigAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, result.ErrorCode);
        Assert.IsNull(result.ErrorMessage);
        Assert.IsNull(result.ErrorException);
    }

    [TestMethod]
    public void ForceRefresh_ShouldInvokeConfigServiceRefreshConfig()
    {
        // Arrange

        this.configServiceMock.Setup(m => m.RefreshConfig()).Returns(RefreshResult.Success());

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var result = instance.ForceRefresh();

        // Assert

        this.configServiceMock.Verify(m => m.RefreshConfig(), Times.Once);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, result.ErrorCode);
        Assert.IsNull(result.ErrorMessage);
        Assert.IsNull(result.ErrorException);
    }

    [TestMethod]
    public async Task ForceRefreshAsync_ShouldInvokeConfigServiceRefreshConfigAsync()
    {
        // Arrange

        this.configServiceMock.Setup(m => m.RefreshConfigAsync(It.IsAny<CancellationToken>())).ReturnsAsync(RefreshResult.Success());

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var result = await instance.ForceRefreshAsync();

        // Assert

        this.configServiceMock.Verify(m => m.RefreshConfigAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, result.ErrorCode);
        Assert.IsNull(result.ErrorMessage);
        Assert.IsNull(result.ErrorException);
    }

    [TestMethod]
    public async Task ForceRefreshAsync_ConfigServiceThrowException_ShouldNotReThrowTheExceptionAndLogsError()
    {
        // Arrange

        var exception = new Exception();

        this.configServiceMock.Setup(m => m.RefreshConfigAsync(It.IsAny<CancellationToken>())).Throws(exception);

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var result = await instance.ForceRefreshAsync();

        // Assert

        this.loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.UnexpectedError, result.ErrorCode);
        Assert.AreEqual(exception.Message, result.ErrorMessage);
        Assert.AreSame(exception, result.ErrorException);
    }

    [TestMethod]
    public void ForceRefresh_ConfigServiceThrowException_ShouldNotReThrowTheExceptionAndLogsError()
    {
        // Arrange

        var exception = new Exception();

        this.configServiceMock.Setup(m => m.RefreshConfig()).Throws(exception);

        IConfigCatClient instance = new ConfigCatClient(
            this.configServiceMock.Object,
            this.loggerMock.Object,
            this.evaluatorMock.Object);

        // Act

        var result = instance.ForceRefresh();

        // Assert

        this.loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.UnexpectedError, result.ErrorCode);
        Assert.AreEqual(exception.Message, result.ErrorMessage);
        Assert.AreSame(exception, result.ErrorException);
    }

    [TestMethod]
    public async Task ForceRefreshAsync_ShouldBeCancelable()
    {
        // Arrange

        const int delayMs = 2000;

        var loggerWrapper = this.loggerMock.Object.AsWrapper();
        var fakeHandler = new FakeHttpClientHandler(HttpStatusCode.OK, "{ }", TimeSpan.FromMilliseconds(delayMs));
        var configFetcher = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", loggerWrapper, new HttpClientConfigFetcher(fakeHandler), false, TimeSpan.FromMilliseconds(delayMs * 2));
        var configCache = new InMemoryConfigCache();
        var cacheParams = new CacheParameters(configCache, cacheKey: null!);
        var configService = new ManualPollConfigService(configFetcher, cacheParams, loggerWrapper);
        var evaluator = new RolloutEvaluator(loggerWrapper);
        using var client = new ConfigCatClient(configService, loggerWrapper, evaluator);

        // Act

        using var cts = new CancellationTokenSource(delayMs / 4);

        Func<Task> action = () => client.ForceRefreshAsync(cts.Token);

        // Assert

        var ex = await Assert.ThrowsExceptionAsync<TaskCanceledException>(action);
        Assert.AreEqual(cts.Token, ex.CancellationToken);
    }

    private static IConfigCatClient CreateClientFromLocalFile(string fileName, User? defaultUser = null)
    {
        return ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(
                Path.Combine("data", fileName),
                autoReload: false,
                OverrideBehaviour.LocalOnly
            );
            options.DefaultUser = defaultUser;
        });
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    [DoNotParallelize]
    public async Task DefaultUser_GetValue(bool isAsync)
    {
        using IConfigCatClient client = CreateClientFromLocalFile("sample_v5.json", new User("a@configcat.com") { Email = "a@configcat.com" });

        Func<string, string, User?, Task<string>> getValueAsync = isAsync
            ? (key, defaultValue, user) => client.GetValueAsync(key, defaultValue, user)
            : (key, defaultValue, user) => Task.FromResult(client.GetValue(key, defaultValue, user));

        const string key = "stringIsInDogDefaultCat";

        // 1. Checks that default user set in options is used for evaluation 
        Assert.AreEqual("Dog", await getValueAsync(key, string.Empty, null));

        client.ClearDefaultUser();

        // 2. Checks that default user can be cleared
        Assert.AreEqual("Cat", await getValueAsync(key, string.Empty, null));

        client.SetDefaultUser(new User("b@configcat.com") { Email = "b@configcat.com" });

        // 3. Checks that default user set on client is used for evaluation 
        Assert.AreEqual("Dog", await getValueAsync(key, string.Empty, null));

        // 4. Checks that default user can be overridden by parameter
        Assert.AreEqual("Cat", await getValueAsync(key, string.Empty, new User("c@configcat.com") { Email = "c@configcat.com" }));
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    [DoNotParallelize]
    public async Task DefaultUser_GetAllValues(bool isAsync)
    {
        using IConfigCatClient client = CreateClientFromLocalFile("sample_v5.json", new User("a@configcat.com") { Email = "a@configcat.com" });

        Func<User?, Task<IReadOnlyDictionary<string, object?>>> getAllValuesAsync = isAsync
            ? user => client.GetAllValuesAsync(user)
            : user => Task.FromResult(client.GetAllValues(user));

        const string key = "stringIsInDogDefaultCat";

        // 1. Checks that default user set in options is used for evaluation 
        Assert.AreEqual("Dog", (await getAllValuesAsync(null))[key]);

        client.ClearDefaultUser();

        // 2. Checks that default user can be cleared
        Assert.AreEqual("Cat", (await getAllValuesAsync(null))[key]);

        client.SetDefaultUser(new User("b@configcat.com") { Email = "b@configcat.com" });

        // 3. Checks that default user set on client is used for evaluation 
        Assert.AreEqual("Dog", (await getAllValuesAsync(null))[key]);

        // 4. Checks that default user can be overridden by parameter
        Assert.AreEqual("Cat", (await getAllValuesAsync(new User("c@configcat.com") { Email = "c@configcat.com" }))[key]);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    [DoNotParallelize]
    public void Get_ReturnsCachedInstance_NoWarning(bool passConfigureToSecondGet)
    {
        // Arrange

        var warnings = new List<string>();

        this.loggerMock
            .Setup(m => m.Log(LogLevel.Warning, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()))
            .Callback(delegate (LogLevel _, LogEventId _, ref FormattableLogMessage msg, Exception _) { warnings.Add(msg.InvariantFormattedMessage); });

        void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = this.loggerMock.Object;
        }

        // Act

        using var client1 = ConfigCatClient.Get("test-67890123456789012/1234567890123456789012", Configure);
        var warnings1 = warnings.ToArray();

        warnings.Clear();
        using var client2 = ConfigCatClient.Get("test-67890123456789012/1234567890123456789012", passConfigureToSecondGet ? Configure : null);
        var warnings2 = warnings.ToArray();

        // Assert

        Assert.AreEqual(1, ConfigCatClient.Instances.GetAliveCount());
        Assert.AreSame(client1, client2);
        Assert.IsFalse(warnings1.Any(msg => msg.Contains("configuration action is ignored")));

        if (passConfigureToSecondGet)
        {
            Assert.IsTrue(warnings2.Any(msg => msg.Contains("configuration action is ignored")));
        }
        else
        {
            Assert.IsFalse(warnings2.Any(msg => msg.Contains("configuration action is ignored")));
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void Dispose_CachedInstanceRemoved()
    {
        // Arrange

        var client1 = ConfigCatClient.Get("test-67890123456789012/1234567890123456789012", options => options.PollingMode = PollingModes.ManualPoll);

        // Act

        var instanceCount1 = ConfigCatClient.Instances.GetAliveCount();

        client1.Dispose();

        var instanceCount2 = ConfigCatClient.Instances.GetAliveCount();

        // Assert

        Assert.AreEqual(1, instanceCount1);
        Assert.AreEqual(0, instanceCount2);
    }

    [TestMethod]
    [DoNotParallelize]
    public void Dispose_CanRemoveCurrentCachedInstanceOnly()
    {
        // Arrange

        var client1 = ConfigCatClient.Get("test-67890123456789012/1234567890123456789012", options => options.PollingMode = PollingModes.ManualPoll);

        // Act

        var instanceCount1 = ConfigCatClient.Instances.GetAliveCount();

        client1.Dispose();

        var instanceCount2 = ConfigCatClient.Instances.GetAliveCount();

        var client2 = ConfigCatClient.Get("test-67890123456789012/1234567890123456789012", options => options.PollingMode = PollingModes.ManualPoll);

        var instanceCount3 = ConfigCatClient.Instances.GetAliveCount();

        client1.Dispose();

        var instanceCount4 = ConfigCatClient.Instances.GetAliveCount();

        client2.Dispose();

        var instanceCount5 = ConfigCatClient.Instances.GetAliveCount();

        // Assert

        Assert.AreEqual(1, instanceCount1);
        Assert.AreEqual(0, instanceCount2);
        Assert.AreEqual(1, instanceCount3);
        Assert.AreEqual(1, instanceCount4);
        Assert.AreEqual(0, instanceCount5);
    }

    [TestMethod]
    [DoNotParallelize]
    public void DisposeAll_CachedInstancesRemoved()
    {
        // Arrange

        var client1 = ConfigCatClient.Get("test1-7890123456789012/1234567890123456789012", options => options.PollingMode = PollingModes.AutoPoll());
        var client2 = ConfigCatClient.Get("test2-7890123456789012/1234567890123456789012", options => options.PollingMode = PollingModes.ManualPoll);

        // Act

        int instanceCount1;

        instanceCount1 = ConfigCatClient.Instances.GetAliveCount();

        GC.KeepAlive(client1);
        GC.KeepAlive(client2);

        ConfigCatClient.DisposeAll();

        var instanceCount2 = ConfigCatClient.Instances.GetAliveCount();

        // Assert

        Assert.AreEqual(2, instanceCount1);
        Assert.AreEqual(0, instanceCount2);
    }

    [TestMethod]
    [DoNotParallelize]
    public void CachedInstancesCanBeGCdWhenNoReferencesAreLeft()
    {
        // Arrange

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CreateClients(out int instanceCount)
        {
            // We need to prevent the auto poll service from raising the ClientReady event from its background work loop
            // because that could interfere with this test: when raising the event, the service acquires a strong reference to the client,
            // which would temporarily prevent the client from being GCd. This could break the test in the case of unlucky timing.
            // Setting maxInitWaitTime to zero prevents this because then the event is raised immediately at creation.
            var client1 = ConfigCatClient.Get("test1-7890123456789012/1234567890123456789012", options => options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.Zero));
            var client2 = ConfigCatClient.Get("test2-7890123456789012/1234567890123456789012", options => options.PollingMode = PollingModes.ManualPoll);

            instanceCount = ConfigCatClient.Instances.GetAliveCount();

            GC.KeepAlive(client1);
            GC.KeepAlive(client2);
        }

        // Act

        CreateClients(out var instanceCount1);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        var instanceCount2 = ConfigCatClient.Instances.GetAliveCount();

        // Assert

        Assert.AreEqual(2, instanceCount1);
        Assert.AreEqual(0, instanceCount2);
    }

    [TestMethod]
    [DoNotParallelize]
    public void CachedInstancesCanBeGCdWhenHookHandlerClosesOverClientInstance()
    {
        // Arrange

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CreateClients(out int instanceCount)
        {
            var client = ConfigCatClient.Get("test1-7890123456789012/1234567890123456789012", options => options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.Zero));

            client.ConfigChanged += (_, e) =>
            {
                client.GetValue("flag", false);
            };

            instanceCount = ConfigCatClient.Instances.GetAliveCount();

            GC.KeepAlive(client);
        }

        // Act

        CreateClients(out var instanceCount1);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        var instanceCount2 = ConfigCatClient.Instances.GetAliveCount();

        // Assert

        Assert.AreEqual(1, instanceCount1);
        Assert.AreEqual(0, instanceCount2);
    }

    [DataRow(nameof(AutoPoll))]
    [DataRow(nameof(LazyLoad))]
    [DataRow(nameof(ManualPoll))]
    [DataTestMethod]
    public async Task OfflineMode_OfflineToOnlineTransition(string pollingMode)
    {
        const string cacheKey = "123";
        var httpETag = 0;

        Func<IConfigFetcher, CacheParameters, LoggerWrapper, IConfigService> configServiceFactory = pollingMode switch
        {
            nameof(AutoPoll) => ((fetcher, cacheParams, loggerWrapper) =>
            {
                // Make sure that config is fetched only once (Task.Delay won't accept a larger interval than int.MaxValue msecs...)
                var pollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromMilliseconds(int.MaxValue));
                return new AutoPollConfigService(pollingMode, this.fetcherMock.Object, cacheParams, loggerWrapper, isOffline: true);
            }),
            nameof(LazyLoad) => ((fetcher, cacheParams, loggerWrapper) =>
            {
                // Make sure that config is fetched only once (Task.Delay won't accept a larger interval than int.MaxValue msecs...)
                var pollingMode = PollingModes.LazyLoad(cacheTimeToLive: TimeSpan.FromMilliseconds(int.MaxValue));
                return new LazyLoadConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper, pollingMode.CacheTimeToLive, isOffline: true);
            }),
            nameof(ManualPoll) => ((fetcher, cacheParams, loggerWrapper) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper, isOffline: true);
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(pollingMode), pollingMode, null)
        };

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: cfg => FetchResult.Success(ConfigHelper.FromString("{}", httpETag: (++httpETag).ToString(CultureInfo.InvariantCulture), timeStamp: ProjectConfig.GenerateTimeStamp())),
            configServiceFactory, out var configService, out _);

        var expectedFetchAsyncCount = 0;

        using (client)
        {
            // 1. Checks that client is initialized to offline mode
            Assert.IsTrue(client.IsOffline);
            Assert.AreEqual(default, configService.GetConfig().HttpETag);
            Assert.AreEqual(default, (await configService.GetConfigAsync()).HttpETag);

            // 2. Checks that repeated calls to SetOffline() have no effect
            client.SetOffline();

            Assert.IsTrue(client.IsOffline);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            // 3. Checks that SetOnline() does enable HTTP calls
            client.SetOnline();

            if (pollingMode == nameof(AutoPoll))
            {
                await Task.Delay(100);
                expectedFetchAsyncCount++;
            }

            Assert.IsFalse(client.IsOffline);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            var etag1 = ParseETagAsInt32(configService.GetConfig().HttpETag);
            if (pollingMode == nameof(LazyLoad))
            {
                expectedFetchAsyncCount++;
            }
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            if (pollingMode == nameof(ManualPoll))
            {
                Assert.AreEqual(0, etag1);
            }
            else
            {
                Assert.AreNotEqual(0, etag1);
            }
            Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

            // 4. Checks that ForceRefresh() initiates a HTTP call in online mode
            var refreshResult = client.ForceRefresh();
            expectedFetchAsyncCount++;

            Assert.IsFalse(client.IsOffline);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            var etag2 = ParseETagAsInt32(configService.GetConfig().HttpETag);
            if (pollingMode == nameof(ManualPoll))
            {
                Assert.AreNotEqual(etag2, etag1);
            }
            else
            {
                Assert.IsTrue(etag2 > etag1);
            }
            Assert.AreEqual(etag2, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

            Assert.IsTrue(refreshResult.IsSuccess);
            Assert.AreEqual(RefreshErrorCode.None, refreshResult.ErrorCode);
            Assert.IsNull(refreshResult.ErrorMessage);
            Assert.IsNull(refreshResult.ErrorException);

            // 5. Checks that ForceRefreshAsync() initiates a HTTP call in online mode
            refreshResult = await client.ForceRefreshAsync();
            expectedFetchAsyncCount++;

            Assert.IsFalse(client.IsOffline);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            var etag3 = ParseETagAsInt32(configService.GetConfig().HttpETag);
            Assert.IsTrue(etag3 > etag2);
            Assert.AreEqual(etag3, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

            Assert.IsTrue(refreshResult.IsSuccess);
            Assert.AreEqual(RefreshErrorCode.None, refreshResult.ErrorCode);
            Assert.IsNull(refreshResult.ErrorMessage);
            Assert.IsNull(refreshResult.ErrorException);
        }

        // 6. Checks that SetOnline() has no effect after client gets disposed
        client.SetOnline();
        Assert.IsTrue(client.IsOffline);
    }

    [DataRow(nameof(AutoPoll))]
    [DataRow(nameof(LazyLoad))]
    [DataRow(nameof(ManualPoll))]
    [DataTestMethod]
    public async Task OfflineMode_OnlineToOfflineTransition(string pollingMode)
    {
        const string cacheKey = "123";
        var httpETag = 0;

        Func<IConfigFetcher, CacheParameters, LoggerWrapper, IConfigService> configServiceFactory = pollingMode switch
        {
            nameof(AutoPoll) => ((fetcher, cacheParams, loggerWrapper) =>
            {
                // Make sure that config is fetched only once (Task.Delay won't accept a larger interval than int.MaxValue msecs...)
                var pollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromMilliseconds(int.MaxValue));
                return new AutoPollConfigService(pollingMode, this.fetcherMock.Object, cacheParams, loggerWrapper, isOffline: false);
            }),
            nameof(LazyLoad) => ((fetcher, cacheParams, loggerWrapper) =>
            {
                // Make sure that config is fetched only once (Task.Delay won't accept a larger interval than int.MaxValue msecs...)
                var pollingMode = PollingModes.LazyLoad(cacheTimeToLive: TimeSpan.FromMilliseconds(int.MaxValue));
                return new LazyLoadConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper, pollingMode.CacheTimeToLive, isOffline: false);
            }),
            nameof(ManualPoll) => ((fetcher, cacheParams, loggerWrapper) =>
            {
                return new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper, isOffline: false);
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(pollingMode), pollingMode, null)
        };

        var client = CreateClientWithMockedFetcher(cacheKey, this.loggerMock, this.fetcherMock,
            onFetch: cfg => FetchResult.Success(ConfigHelper.FromString("{}", httpETag: (++httpETag).ToString(CultureInfo.InvariantCulture), timeStamp: ProjectConfig.GenerateTimeStamp())),
            configServiceFactory, out var configService, out var configCache);

        var expectedFetchAsyncCount = 0;

        using (client)
        {
            // 1. Checks that client is initialized to online mode
            Assert.IsFalse(client.IsOffline);

            if (pollingMode == nameof(AutoPoll))
            {
                Assert.IsTrue(await ((AutoPollConfigService)configService).InitializationTask);
                expectedFetchAsyncCount++;
            }

            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            var etag1 = ParseETagAsInt32(configService.GetConfig().HttpETag);
            if (pollingMode == nameof(LazyLoad))
            {
                expectedFetchAsyncCount++;
            }
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            if (pollingMode == nameof(ManualPoll))
            {
                Assert.AreEqual(0, etag1);
            }
            else
            {
                Assert.AreNotEqual(0, etag1);
            }
            Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

            // 2. Checks that repeated calls to SetOnline() have no effect 
            client.SetOnline();

            Assert.IsFalse(client.IsOffline);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            // 3. Checks that SetOffline() does disable HTTP calls
            client.SetOffline();

            Assert.IsTrue(client.IsOffline);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            if (pollingMode == nameof(LazyLoad))
            {
                // We make sure manually that the cached config is expired for the next GetConfig() call
                var cachedConfig = configCache.Get(cacheKey).Config;
                cachedConfig = cachedConfig.With(cachedConfig.TimeStamp - TimeSpan.FromMilliseconds(int.MaxValue * 2.0));
                configCache.Set(cacheKey, cachedConfig);
            }

            Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
            Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

            // 4. Checks that ForceRefresh() does not initiate a HTTP call in offline mode
            var refreshResult = client.ForceRefresh();

            Assert.IsTrue(client.IsOffline);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
            Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

            Assert.IsFalse(refreshResult.IsSuccess);
            Assert.AreEqual(RefreshErrorCode.OfflineClient, refreshResult.ErrorCode);
            StringAssert.Contains(refreshResult.ErrorMessage, "offline mode");
            Assert.IsNull(refreshResult.ErrorException);

            // 5. Checks that ForceRefreshAsync() does not initiate a HTTP call in offline mode
            refreshResult = await client.ForceRefreshAsync();

            Assert.IsTrue(client.IsOffline);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedFetchAsyncCount));

            Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
            Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

            Assert.IsFalse(refreshResult.IsSuccess);
            Assert.AreEqual(RefreshErrorCode.OfflineClient, refreshResult.ErrorCode);
            StringAssert.Contains(refreshResult.ErrorMessage, "offline mode");
            Assert.IsNull(refreshResult.ErrorException);
        }

        // 6. Checks that SetOnline() has no effect after client gets disposed
        client.SetOnline();
        Assert.IsTrue(client.IsOffline);
    }

    [TestMethod]
    public async Task Hooks_MockedClientRaisesEvents()
    {
        const string cacheKey = "123";
        var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");

        var clientReadyEventCount = 0;
        var configFetchedEvents = new List<ConfigFetchedEventArgs>();
        var configChangedEvents = new List<ConfigChangedEventArgs>();
        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        var errorEvents = new List<ConfigCatClientErrorEventArgs>();

        var hooks = new Hooks();
        hooks.ClientReady += (s, e) => clientReadyEventCount++;
        hooks.ConfigFetched += (s, e) => configFetchedEvents.Add(e);
        hooks.ConfigChanged += (s, e) => configChangedEvents.Add(e);
        hooks.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);
        hooks.Error += (s, e) => errorEvents.Add(e);

        var loggerWrapper = this.loggerMock.Object.AsWrapper(hooks: hooks);

        var errorException = new HttpRequestException();

        var onFetch = (ProjectConfig latestConfig, CancellationToken _) =>
        {
            var logMessage = loggerWrapper.FetchFailedDueToUnexpectedError(errorException);
            return FetchResult.Failure(latestConfig, RefreshErrorCode.HttpRequestFailure, errorMessage: logMessage.ToLazyString(), errorException: errorException);
        };
        this.fetcherMock.Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>())).ReturnsAsync(onFetch);

        var configCache = new InMemoryConfigCache();

        var cacheParams = new CacheParameters(configCache, cacheKey);

        var configService = new ManualPollConfigService(this.fetcherMock.Object, cacheParams, loggerWrapper, hooks: hooks);

        // 1. Client gets created
        var client = new ConfigCatClient(configService, this.loggerMock.Object, new RolloutEvaluator(loggerWrapper), hooks: hooks);

        var cacheState = await client.WaitForReadyAsync();
        Assert.AreEqual(ClientCacheState.NoFlagData, cacheState);

        Assert.AreEqual(1, clientReadyEventCount);
        Assert.AreEqual(0, configFetchedEvents.Count);
        Assert.AreEqual(0, configChangedEvents.Count);
        Assert.AreEqual(0, flagEvaluatedEvents.Count);
        Assert.AreEqual(0, errorEvents.Count);

        // 2. Fetch fails
        await client.ForceRefreshAsync();

        Assert.AreEqual(1, configFetchedEvents.Count);
        Assert.IsTrue(configFetchedEvents[0].IsInitiatedByUser);
        Assert.IsFalse(configFetchedEvents[0].Result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.HttpRequestFailure, configFetchedEvents[0].Result.ErrorCode);
        Assert.AreEqual(0, configChangedEvents.Count);
        Assert.AreEqual(1, errorEvents.Count);
        Assert.IsNotNull(errorEvents[0].Message);
        Assert.AreSame(errorException, errorEvents[0].Exception);

        // 3. Fetch succeeds
        var config = ConfigHelper.FromFile(configJsonFilePath, httpETag: "12345", ProjectConfig.GenerateTimeStamp());

        onFetch = (_, _) => FetchResult.Success(config);
        this.fetcherMock.Reset();
        this.fetcherMock.Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>())).ReturnsAsync(onFetch);

        await client.ForceRefreshAsync();

        Assert.AreEqual(2, configFetchedEvents.Count);
        Assert.IsTrue(configFetchedEvents[1].IsInitiatedByUser);
        Assert.IsTrue(configFetchedEvents[1].Result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, configFetchedEvents[1].Result.ErrorCode);
        Assert.AreEqual(1, configChangedEvents.Count);
        Assert.AreSame(config.Config, configChangedEvents[0].NewConfig);

        // 4. All flags are evaluated
        var keys = await client.GetAllKeysAsync();
        var evaluationDetails = new List<EvaluationDetails>();
        foreach (var key in keys)
        {
            evaluationDetails.Add(await client.GetValueDetailsAsync<object>(key, defaultValue: ""));
        }

        Assert.AreEqual(evaluationDetails.Count, flagEvaluatedEvents.Count);
        CollectionAssert.AreEqual(evaluationDetails, flagEvaluatedEvents.Select(e => e.EvaluationDetails).ToArray());

        // 5. Client gets disposed of
        client.Dispose();

        Assert.AreEqual(1, clientReadyEventCount);
        Assert.AreEqual(2, configFetchedEvents.Count);
        Assert.AreEqual(1, configChangedEvents.Count);
        Assert.AreEqual(evaluationDetails.Count, flagEvaluatedEvents.Count);
        Assert.AreEqual(1, errorEvents.Count);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    [DoNotParallelize]
    public async Task Hooks_RealClientRaisesEvents(bool subscribeViaOptions)
    {
        var clientReadyCallCount = 0;
        var configFetchedEvents = new List<ConfigFetchedEventArgs>();
        var configChangedEvents = new List<ConfigChangedEventArgs>();
        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        var errorEvents = new List<ConfigCatClientErrorEventArgs>();

        EventHandler<ClientReadyEventArgs> handleClientReady = (s, e) => clientReadyCallCount++;
        EventHandler<ConfigFetchedEventArgs> handleConfigFetched = (s, e) => configFetchedEvents.Add(e);
        EventHandler<ConfigChangedEventArgs> handleConfigChanged = (s, e) => configChangedEvents.Add(e);
        EventHandler<FlagEvaluatedEventArgs> handleFlagEvaluated = (s, e) => flagEvaluatedEvents.Add(e);
        EventHandler<ConfigCatClientErrorEventArgs> handleError = (s, e) => errorEvents.Add(e);

        void Subscribe(IProvidesHooks hooks)
        {
            hooks.ClientReady += handleClientReady;
            hooks.ConfigFetched += handleConfigFetched;
            hooks.ConfigChanged += handleConfigChanged;
            hooks.FlagEvaluated += handleFlagEvaluated;
            hooks.Error += handleError;
        }

        void Unsubscribe(IProvidesHooks hooks)
        {
            hooks.ClientReady -= handleClientReady;
            hooks.ConfigFetched -= handleConfigFetched;
            hooks.ConfigChanged -= handleConfigChanged;
            hooks.FlagEvaluated -= handleFlagEvaluated;
            hooks.Error -= handleError;
        }

        // 1. Client gets created
        var client = ConfigCatClient.Get(BasicConfigCatClientIntegrationTests.SDKKEY, options =>
        {
            if (subscribeViaOptions)
            {
                Subscribe(options);
                Unsubscribe(options);
                Subscribe(options);
                Subscribe(options);
            }

            options.PollingMode = PollingModes.ManualPoll;
        });

        if (!subscribeViaOptions)
        {
            Subscribe(client);
            Unsubscribe(client);
            Subscribe(client);
            Subscribe(client);
        }

        var cacheState = await client.WaitForReadyAsync();
        Assert.AreEqual(ClientCacheState.NoFlagData, cacheState);

        Assert.AreEqual(subscribeViaOptions ? 2 : 0, clientReadyCallCount);
        Assert.AreEqual(0, configFetchedEvents.Count);
        Assert.AreEqual(0, configChangedEvents.Count);
        Assert.AreEqual(0, flagEvaluatedEvents.Count);
        Assert.AreEqual(0, errorEvents.Count);

        // 2. Fetch succeeds
        await client.ForceRefreshAsync();

        Assert.AreEqual(2, configFetchedEvents.Count);
        Assert.AreSame(configFetchedEvents[0], configFetchedEvents[1]);
        Assert.IsTrue(configFetchedEvents[0].IsInitiatedByUser);
        Assert.IsTrue(configFetchedEvents[0].Result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, configFetchedEvents[1].Result.ErrorCode);
        Assert.AreEqual(2, configChangedEvents.Count);
        Assert.IsTrue(configChangedEvents[0].NewConfig.Settings.Any());
        Assert.AreSame(configChangedEvents[0], configChangedEvents[1]);

        // 3. Non-existent flag is evaluated

        const string invalidKey = "<invalid-key>";

        await client.GetValueAsync(invalidKey, defaultValue: (object?)null);

        Assert.AreEqual(2, errorEvents.Count);
        Assert.IsNotNull(errorEvents[0].Message);
        Assert.IsNull(errorEvents[0].Exception);
        Assert.AreSame(errorEvents[0], errorEvents[1]);

        Assert.AreEqual(2, flagEvaluatedEvents.Count);
        Assert.AreEqual(invalidKey, flagEvaluatedEvents[0].EvaluationDetails.Key);
        Assert.AreEqual(errorEvents[0].Message, flagEvaluatedEvents[0].EvaluationDetails.ErrorMessage);
        Assert.IsNull(errorEvents[0].Exception);
        Assert.AreSame(flagEvaluatedEvents[0], flagEvaluatedEvents[1]);

        flagEvaluatedEvents.Clear();

        // 4. All flags are evaluated
        var keys = await client.GetAllKeysAsync();
        var evaluationDetails = new List<EvaluationDetails>();
        foreach (var key in keys)
        {
            evaluationDetails.Add(await client.GetValueDetailsAsync<object>(key, defaultValue: ""));
        }

        Assert.AreEqual(evaluationDetails.Count * 2, flagEvaluatedEvents.Count);
        CollectionAssert.AreEqual(
            evaluationDetails.SelectMany(ed => Enumerable.Repeat(ed, 2)).ToArray(),
            flagEvaluatedEvents.Select(e => e.EvaluationDetails).ToArray());

        // 5. Client gets disposed of
        client.Dispose();

        Assert.AreEqual(subscribeViaOptions ? 2 : 0, clientReadyCallCount);
        Assert.AreEqual(2, configFetchedEvents.Count);
        Assert.AreEqual(2, configChangedEvents.Count);
        Assert.AreEqual(evaluationDetails.Count * 2, flagEvaluatedEvents.Count);
        Assert.AreEqual(2, errorEvents.Count);
    }

    [TestMethod]
    public async Task LogFilter_Works()
    {
        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Info);

        var options = new ConfigCatClientOptions
        {
            Logger = logger,
            LogFilter = (LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception) => eventId != 3001,
            FlagOverrides = FlagOverrides.LocalFile(Path.Combine("data", "sample_variationid_v5.json"), autoReload: false, OverrideBehaviour.LocalOnly)
        };

        using var client = new ConfigCatClient("localonly", options);

        var actualValue = await client.GetValueAsync("boolean", (bool?)null);
        Assert.IsFalse(actualValue);

        Assert.AreEqual(1, logEvents.Count);
        Assert.AreEqual(LogLevel.Info, logEvents[0].Level);
        Assert.AreEqual(5000, logEvents[0].EventId);
        Assert.IsNull(logEvents[0].Exception);
    }

    private static IConfigCatClient CreateClientWithMockedFetcher(string cacheKey,
        Mock<IConfigCatLogger> loggerMock,
        Mock<IConfigFetcher> fetcherMock,
        Func<ProjectConfig, FetchResult> onFetch,
        Func<IConfigFetcher, CacheParameters, LoggerWrapper, IConfigService> configServiceFactory,
        out IConfigService configService,
        out ConfigCache configCache)
    {
        return CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock, onFetch,
            configServiceFactory: (fetcher, cacheParams, logger, hooks) => configServiceFactory(fetcher, cacheParams, logger),
            hooks: null, out configService, out configCache);
    }

    private static IConfigCatClient CreateClientWithMockedFetcher(string cacheKey,
        Mock<IConfigCatLogger> loggerMock,
        Mock<IConfigFetcher> fetcherMock,
        Func<ProjectConfig, FetchResult> onFetch,
        Func<IConfigFetcher, CacheParameters, LoggerWrapper, Hooks?, IConfigService> configServiceFactory,
        Hooks? hooks,
        out IConfigService configService,
        out ConfigCache configCache)
    {
        return CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock, onFetch, configServiceFactory,
            evaluatorFactory: null, hooks, out configService, out configCache);
    }

    private static IConfigCatClient CreateClientWithMockedFetcher(string cacheKey,
        Mock<IConfigCatLogger> loggerMock,
        Mock<IConfigFetcher> fetcherMock,
        Func<ProjectConfig, FetchResult> onFetch,
        Func<IConfigFetcher, CacheParameters, LoggerWrapper, Hooks?, IConfigService> configServiceFactory,
        Func<LoggerWrapper, IRolloutEvaluator>? evaluatorFactory,
        Hooks? hooks,
        out IConfigService configService,
        out ConfigCache configCache)
    {
        var configCacheLocal = configCache = new InMemoryConfigCache();

        return CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
            onFetch, onFetchAsync: (pc, _) => Task.FromResult(onFetch(pc)),
            configServiceFactory, evaluatorFactory,
            configCacheFactory: _ => configCacheLocal,
            overrideDataSourceFactory: null, hooks, out configService);
    }

    private static IConfigCatClient CreateClientWithMockedFetcher(string cacheKey,
        Mock<IConfigCatLogger> loggerMock,
        Mock<IConfigFetcher> fetcherMock,
        Func<ProjectConfig, FetchResult> onFetch,
        Func<ProjectConfig, CancellationToken, Task<FetchResult>> onFetchAsync,
        Func<IConfigFetcher, CacheParameters, LoggerWrapper, Hooks?, IConfigService> configServiceFactory,
        Func<LoggerWrapper, IRolloutEvaluator>? evaluatorFactory,
        Func<LoggerWrapper, ConfigCache>? configCacheFactory,
        Func<LoggerWrapper, Tuple<OverrideBehaviour, IOverrideDataSource>>? overrideDataSourceFactory,
        Hooks? hooks,
        out IConfigService configService)
    {
        fetcherMock.Setup(m => m.Fetch(It.IsAny<ProjectConfig>())).Returns(onFetch);
        fetcherMock.Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>())).Returns((ProjectConfig pc, CancellationToken ct) => onFetchAsync(pc, ct));

        var loggerWrapper = loggerMock.Object.AsWrapper();

        var configCache = configCacheFactory is not null ? configCacheFactory(loggerWrapper) : new InMemoryConfigCache();
        var cacheParams = new CacheParameters(configCache, cacheKey);

        var evaluator = evaluatorFactory is not null ? evaluatorFactory(loggerWrapper) : new RolloutEvaluator(loggerWrapper);
        var overrideDataSource = overrideDataSourceFactory?.Invoke(loggerWrapper);

        configService = configServiceFactory(fetcherMock.Object, cacheParams, loggerWrapper, hooks);
        return new ConfigCatClient(configService, loggerMock.Object, evaluator, overrideDataSource?.Item1, overrideDataSource?.Item2, hooks: hooks);
    }

    private static int ParseETagAsInt32(string? etag)
    {
        return int.TryParse(etag, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    internal class FakeConfigService : ConfigServiceBase, IConfigService
    {
        public byte DisposeCount { get; private set; }

        public FakeConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper logger)
            : base(configFetcher, cacheParameters, logger, isOffline: false, hooks: null)
        {
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            base.Dispose(disposing);
        }

        public Task<ClientCacheState> ReadyTask => Task.FromResult(ClientCacheState.NoFlagData);

        public ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<ProjectConfig>(ProjectConfig.Empty);
        }

        public override ValueTask<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<RefreshResult>(RefreshConfig());
        }

        public ProjectConfig GetConfig()
        {
            return ProjectConfig.Empty;
        }

        public override RefreshResult RefreshConfig()
        {
            return RefreshResult.Success();
        }

        public override ClientCacheState GetCacheState(ProjectConfig cachedConfig) => ClientCacheState.NoFlagData;
    }
}
