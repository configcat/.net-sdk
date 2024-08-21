using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ConfigServiceTests
{
    private static ProjectConfig CreateExpiredPc(DateTime timeStamp, TimeSpan expiration, string configJson = "{}", string httpETag = "\"67890\"") =>
        ConfigHelper.FromString(configJson, httpETag, timeStamp - expiration - TimeSpan.FromSeconds(1));

    private static ProjectConfig CreateUpToDatePc(DateTime timeStamp, TimeSpan expiration, string configJson = "{}", string httpETag = "\"abcdef\"") =>
        ConfigHelper.FromString(configJson, httpETag, timeStamp - expiration + TimeSpan.FromSeconds(1));

    private static ProjectConfig CreateFreshPc(DateTime timeStamp, string configJson = "{}", string httpETag = "\"12345\"") =>
        ConfigHelper.FromString(configJson, httpETag, timeStamp);

    private readonly Mock<IConfigFetcher> fetcherMock = new(MockBehavior.Strict);
    private readonly Mock<ConfigCache> cacheMock = new(MockBehavior.Strict);
    private readonly Mock<IConfigCatLogger> loggerMock = new(MockBehavior.Loose);

    [TestInitialize]
    public void TestInitialize()
    {
        this.fetcherMock.Reset();
        this.cacheMock.Reset();
        this.loggerMock.Reset();
    }

    [TestMethod]
    public async Task LazyLoadConfigService_GetConfigAsync_ReturnsExpiredContent_ShouldInvokeFetchAndCacheSet()
    {
        // Arrange

        var cacheTimeToLive = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, cacheTimeToLive);
        var fetchedPc = CreateFreshPc(timeStamp);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask))
            .Verifiable();

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(fetchedPc))
            .Verifiable();

        using var service = new LazyLoadConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            cacheTimeToLive);

        // Act

        var projectConfig = await service.GetConfigAsync();

        // Assert

        Assert.AreEqual(fetchedPc, projectConfig);

        this.fetcherMock.VerifyAll();
        this.cacheMock.VerifyAll();
    }

    [TestMethod]
    public async Task LazyLoadConfigService_GetConfigAsync_ReturnsNotExpiredContent_ShouldNotInvokeFetchAndCacheSet()
    {
        // Arrange

        var cacheTimeToLive = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateUpToDatePc(timeStamp, cacheTimeToLive);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        using var service = new LazyLoadConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            cacheTimeToLive);

        // Act

        var projectConfig = await service.GetConfigAsync();

        // Assert

        Assert.AreEqual(cachedPc, projectConfig);

        this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task LazyLoadConfigService_RefreshConfigAsync_ShouldNotInvokeCacheGetAndFetchAndCacheSet()
    {
        // Arrange

        var cacheTimeToLive = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, cacheTimeToLive);
        var fetchedPc = CreateFreshPc(timeStamp);

        byte callOrder = 1;

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc)
            .Callback(() => Assert.IsTrue(callOrder++ is 1 or 2))
            .Verifiable();

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(fetchedPc))
            .Callback(() => Assert.AreEqual(3, callOrder++))
            .Verifiable();

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask))
            .Callback(() => Assert.AreEqual(4, callOrder))
            .Verifiable();

        using var service = new LazyLoadConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            cacheTimeToLive);

        // Act

        await service.RefreshConfigAsync();

        // Assert

        this.fetcherMock.VerifyAll();
        this.cacheMock.VerifyAll();
    }

    [TestMethod]
    public async Task LazyLoadConfigService_RefreshConfigAsync_ConfigChanged_ShouldRaiseEvent()
    {
        // Arrange

        var cacheTimeToLive = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, cacheTimeToLive);
        var fetchedPc = CreateFreshPc(timeStamp);

        var hooks = new Hooks();

        var configChangedEvents = new ConcurrentQueue<ConfigChangedEventArgs>();
        hooks.ConfigChanged += (s, e) => configChangedEvents.Enqueue(e);

        var configFetchedEvents = new ConcurrentQueue<ConfigFetchedEventArgs>();
        hooks.ConfigFetched += (s, e) => configFetchedEvents.Enqueue(e);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask));

        using var service = new LazyLoadConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            cacheTimeToLive,
            hooks: hooks);

        // Act

        await service.RefreshConfigAsync();

        GC.KeepAlive(hooks);

        // Assert

        Assert.IsTrue(configChangedEvents.TryDequeue(out var configChangedEvent));
        Assert.AreSame(fetchedPc.Config, configChangedEvent.NewConfig);
        Assert.AreEqual(0, configChangedEvents.Count);

        Assert.AreEqual(1, configFetchedEvents.Count);
        Assert.IsTrue(configFetchedEvents.TryDequeue(out var configFetchedEvent));
        Assert.IsTrue(configFetchedEvent.IsInitiatedByUser);
        Assert.IsTrue(configFetchedEvent.Result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, configFetchedEvent.Result.ErrorCode);
    }

    [TestMethod]
    public async Task AutoPollConfigService_GetConfigAsync_WithoutTimerWithCachedConfig_ShouldInvokeCacheGet1xAndSetNeverFetchNever()
    {
        // Arrange

        var pollInterval = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateUpToDatePc(timeStamp, pollInterval);
        var fetchedPc = CreateFreshPc(timeStamp);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()))
            .Callback(() => cachedPc = fetchedPc)
            .Returns(default(ValueTask));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime: pollInterval);
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: false);

        this.cacheMock.Invocations.Clear();

        // Act

        await service.GetConfigAsync();

        // Assert

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()), Times.Never);
        this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task AutoPollConfigService_GetConfigAsync_WithTimer_ShouldInvokeFetchAndCacheSetAndCacheGet3x()
    {
        // Arrange

        var pollInterval = TimeSpan.FromSeconds(5);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateUpToDatePc(timeStamp, pollInterval);
        var fetchedPc = CreateFreshPc(timeStamp);

        var wd = new ManualResetEventSlim(false);

        this.cacheMock.SetupGet(m => m.LocalCachedConfig).Returns(cachedPc);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()))
            .Callback(() => wd.Set())
            .Returns(default(ValueTask));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime: TimeSpan.FromSeconds(0));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: true);

        // Act

        wd.Wait(pollInterval + pollInterval);

        await service.GetConfigAsync();
        service.Dispose();

        // Assert

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()), Times.Once);
        this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AutoPollConfigService_RefreshConfigAsync_ShouldOnceInvokeCacheGetAndFetchAndCacheSet()
    {
        // Arrange

        var pollInterval = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateUpToDatePc(timeStamp, pollInterval);
        var fetchedPc = CreateFreshPc(timeStamp);

        this.cacheMock.SetupGet(m => m.LocalCachedConfig).Returns(cachedPc);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime: TimeSpan.FromSeconds(0));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: false);

        this.cacheMock.Invocations.Clear();

        // Act

        await service.RefreshConfigAsync();

        // Assert

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()), Times.Once);
        this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AutoPollConfigService_Dispose_ShouldStopTimer()
    {
        // Arrange

        var pollInterval = TimeSpan.FromSeconds(1);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, pollInterval);

        long counter = 0;
        long e1, e2;

        this.cacheMock.SetupGet(m => m.LocalCachedConfig).Returns(cachedPc);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref counter))
            .ReturnsAsync(FetchResult.Success(cachedPc));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime: TimeSpan.FromSeconds(0));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: ""),
            this.loggerMock.Object.AsWrapper(),
            startTimer: false);

        // Act
        await Task.Delay(pollInterval + pollInterval);
        e1 = Interlocked.Read(ref counter);
        service.Dispose();

        // Assert

        await Task.Delay(config.PollInterval + config.PollInterval);
        e2 = Interlocked.Read(ref counter);
        Console.WriteLine(e2 - e1);
        Assert.IsTrue(e2 - e1 <= 1);
    }

    [TestMethod]
    public async Task AutoPollConfigService_WithoutTimer_InvokeDispose_ShouldDisposeService()
    {
        // Arrange

        var pollInterval = TimeSpan.FromSeconds(1);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, pollInterval);

        long counter = -1;
        long e1;

        this.cacheMock.SetupGet(m => m.LocalCachedConfig).Returns(cachedPc);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref counter))
            .ReturnsAsync(FetchResult.Success(cachedPc));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime: TimeSpan.FromSeconds(0));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: false);

        // Act
        await Task.Delay(pollInterval + pollInterval);
        e1 = Interlocked.Read(ref counter);
        service.Dispose();

        // Assert
        Assert.AreEqual(-1, e1);
    }

    [TestMethod]
    public async Task ManualPollConfigService_GetConfigAsync_ShouldInvokeCacheGet()
    {
        // Arrange

        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, TimeSpan.Zero);

        var hooks = new Hooks();

        var clientReadyEventCount = 0;
        hooks.ClientReady += (s, e) => Interlocked.Increment(ref clientReadyEventCount);

        var configFetchedEventCount = 0;
        hooks.ConfigFetched += (s, e) => Interlocked.Increment(ref configFetchedEventCount);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        using var service = new ManualPollConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            hooks: hooks);

        this.cacheMock.Invocations.Clear();

        // Act

        var projectConfig = await service.GetConfigAsync();

        GC.KeepAlive(hooks);

        // Assert

        Assert.AreEqual(cachedPc, projectConfig);

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));

        Assert.AreEqual(0, Volatile.Read(ref configFetchedEventCount));
    }

    [TestMethod]
    public async Task ManualPollConfigService_RefreshConfigAsync_ShouldInvokeCacheGet()
    {
        // Arrange

        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, TimeSpan.Zero);
        var fetchedPc = CreateFreshPc(timeStamp);

        var hooks = new Hooks();

        var clientReadyEventCount = 0;
        hooks.ClientReady += (s, e) => Interlocked.Increment(ref clientReadyEventCount);

        var configFetchedEvents = new ConcurrentQueue<ConfigFetchedEventArgs>();
        hooks.ConfigFetched += (s, e) => configFetchedEvents.Enqueue(e);

        byte callOrder = 1;

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc)
            .Callback(() => Assert.IsTrue(callOrder++ is 1 or 2));

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(fetchedPc))
            .Callback(() => Assert.AreEqual(3, callOrder++));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()))
            .Callback(() => Assert.AreEqual(4, callOrder++))
            .Returns(default(ValueTask));

        using var service = new ManualPollConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            hooks: hooks);

        // Act

        await service.RefreshConfigAsync();

        GC.KeepAlive(hooks);

        // Assert

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));

        Assert.AreEqual(1, configFetchedEvents.Count);
        Assert.IsTrue(configFetchedEvents.TryDequeue(out var configFetchedEvent));
        Assert.IsTrue(configFetchedEvent.IsInitiatedByUser);
        Assert.IsTrue(configFetchedEvent.Result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, configFetchedEvent.Result.ErrorCode);
    }

    [TestMethod]
    public async Task ManualPollConfigService_RefreshConfigAsync_ConfigChanged_ShouldRaiseEvent()
    {
        // Arrange

        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, TimeSpan.Zero);
        var fetchedPc = CreateFreshPc(timeStamp);

        var hooks = new Hooks();

        var configChangedEvents = new ConcurrentQueue<ConfigChangedEventArgs>();
        hooks.ConfigChanged += (s, e) => configChangedEvents.Enqueue(e);

        var configFetchedEvents = new ConcurrentQueue<ConfigFetchedEventArgs>();
        hooks.ConfigFetched += (s, e) => configFetchedEvents.Enqueue(e);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask));

        using var service = new ManualPollConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            hooks: hooks);

        // Act

        await service.RefreshConfigAsync();

        GC.KeepAlive(hooks);

        // Assert

        Assert.IsTrue(configChangedEvents.TryDequeue(out var configChangedEvent));
        Assert.AreSame(fetchedPc.Config, configChangedEvent.NewConfig);
        Assert.AreEqual(0, configChangedEvents.Count);

        Assert.AreEqual(1, configFetchedEvents.Count);
        Assert.IsTrue(configFetchedEvents.TryDequeue(out var configFetchedEvent));
        Assert.IsTrue(configFetchedEvent.IsInitiatedByUser);
        Assert.IsTrue(configFetchedEvent.Result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, configFetchedEvent.Result.ErrorCode);
    }

    [TestMethod]
    public void ConfigService_InvokeDisposeManyTimes_ShouldInvokeFetcherDisposeExactlyOnce()
    {
        // Arrange

        var configFetcherMock = new Mock<IConfigFetcher>();
        configFetcherMock
            .Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.NotModified(ProjectConfig.Empty));

        var configFetcherMockDispose = configFetcherMock.As<IDisposable>();

        configFetcherMockDispose.Setup(m => m.Dispose());

        var configServiceMock = new Mock<ConfigServiceBase>(
            MockBehavior.Loose,
            new object?[]
            {
                configFetcherMock.Object,
                new CacheParameters(new InMemoryConfigCache(), cacheKey: null!),
                this.loggerMock.Object.AsWrapper(),
                false,
                null
            })
        {
            CallBase = true
        };

        var configService = configServiceMock.Object as IDisposable;

        // Act

        configService.Dispose();
        configService.Dispose();

        // Assert

        configFetcherMockDispose.Verify(m => m.Dispose(), Times.Once);
    }

    [TestMethod]
    public void ConfigService_WithNonDisposableConfigFetcher_DisposeShouldWork()
    {
        // Arrange

        var configFetcherMock = new Mock<IConfigFetcher>();
        configFetcherMock
            .Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.NotModified(ProjectConfig.Empty));

        var configServiceMock = new Mock<ConfigServiceBase>(
            MockBehavior.Loose,
            new object?[]
            {
                configFetcherMock.Object,
                new CacheParameters(new InMemoryConfigCache(), cacheKey: null!),
                new CounterLogger().AsWrapper(),
                false,
                null
            })
        {
            CallBase = true
        };

        var configService = configServiceMock.Object as IDisposable;

        // Act

        configService.Dispose();
    }

    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    [DataTestMethod]
    public async Task AutoPollConfigService_GetConfig_ReturnsCachedConfigWhenCachedConfigIsNotExpired(bool isAsync, bool waitForClientReady)
    {
        // Arrange 

        var pollInterval = TimeSpan.FromSeconds(30);
        var maxInitWaitTime = TimeSpan.FromSeconds(pollInterval.TotalSeconds / 2);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateUpToDatePc(timeStamp, pollInterval);
        var fetchedPc = CreateFreshPc(timeStamp);

        var hooks = new Hooks();

        var clientReadyCalled = false;
        hooks.ClientReady += (s, e) => Volatile.Write(ref clientReadyCalled, true);

        var configFetchedEventCount = 0;
        hooks.ConfigFetched += (s, e) => Interlocked.Increment(ref configFetchedEventCount);

        var cache = new InMemoryConfigCache();
        cache.Set(null!, cachedPc);

        this.fetcherMock.Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.Success(fetchedPc));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime);
        var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(cache, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: true,
            hooks: hooks);

        // Act

        ProjectConfig actualPc;
        using (service)
        {
            if (waitForClientReady)
            {
                await service.ReadyTask;
            }

            actualPc = isAsync ? await service.GetConfigAsync() : service.GetConfig();
        }

        GC.KeepAlive(hooks);

        // Assert

        Assert.AreEqual(cachedPc, actualPc);

        this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Never);

        if (waitForClientReady)
        {
            Assert.IsTrue(clientReadyCalled);
        }

        Assert.AreEqual(0, Volatile.Read(ref configFetchedEventCount));
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task AutoPollConfigService_GetConfig_FetchesConfigWhenCachedConfigIsExpired(bool isAsync)
    {
        // Arrange 

        var pollInterval = TimeSpan.FromSeconds(30);
        var maxInitWaitTime = TimeSpan.FromSeconds(pollInterval.TotalSeconds / 2);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, pollInterval);
        var fetchedPc = CreateFreshPc(timeStamp);

        var hooks = new Hooks();

        var clientReadyTcs = new TaskCompletionSource<object?>();
        hooks.ClientReady += (s, e) => clientReadyTcs.TrySetResult(default);

        var configFetchedEvents = new ConcurrentQueue<ConfigFetchedEventArgs>();
        hooks.ConfigFetched += (s, e) => configFetchedEvents.Enqueue(e);

        var cache = new InMemoryConfigCache();
        cache.Set(null!, cachedPc);

        this.fetcherMock.Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.Success(fetchedPc));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime);
        var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(cache, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: true,
            hooks: hooks);

        // Act

        bool clientReadyCalled;
        ProjectConfig actualPc;
        using (service)
        {
            actualPc = isAsync ? await service.GetConfigAsync() : service.GetConfig();

            // Allow some time for other initalization callbacks to execute.
            using var cts = new CancellationTokenSource();
            var clientReadyTask = Task.Run(async () => await clientReadyTcs.Task);
            var task = await Task.WhenAny(clientReadyTask, Task.Delay(maxInitWaitTime, cts.Token));
            cts.Cancel();
            clientReadyCalled = task == clientReadyTask && task.Status == TaskStatus.RanToCompletion;

            await Task.Yield();

            // Wait for the hook event handlers to execute (as that might not happen if the service got disposed immediately).
            SpinWait.SpinUntil(() => configFetchedEvents.TryPeek(out _), TimeSpan.FromSeconds(1));
        }

        GC.KeepAlive(hooks);

        // Assert

        Assert.AreEqual(fetchedPc, actualPc);

        this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsTrue(clientReadyCalled);

        Assert.AreEqual(1, configFetchedEvents.Count);
        Assert.IsTrue(configFetchedEvents.TryDequeue(out var configFetchedEvent));
        Assert.IsFalse(configFetchedEvent.IsInitiatedByUser);
        Assert.IsTrue(configFetchedEvent.Result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, configFetchedEvent.Result.ErrorCode);
    }

    [DataRow(false, false, true)]
    [DataRow(true, false, true)]
    [DataRow(false, true, true)]
    [DataRow(true, true, true)]
    [DataRow(false, true, false)]
    [DataRow(true, true, false)]
    [DataTestMethod]
    public async Task AutoPollConfigService_GetConfig_ReturnsExpiredConfigWhenCantRefreshWithinMaxInitWaitTime(bool isAsync, bool failure, bool updateTimeStamp)
    {
        // Arrange 

        var pollInterval = TimeSpan.FromSeconds(5);
        var maxInitWaitTime = pollInterval + pollInterval;
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, pollInterval);
        var fetchedPc = updateTimeStamp ? cachedPc.With(timeStamp) : cachedPc;

        var hooks = new Hooks();

        var clientReadyTcs = new TaskCompletionSource<object?>();
        hooks.ClientReady += (s, e) => clientReadyTcs.TrySetResult(default);

        var configFetchedEvents = new ConcurrentQueue<ConfigFetchedEventArgs>();
        hooks.ConfigFetched += (s, e) => configFetchedEvents.Enqueue(e);

        var cache = new InMemoryConfigCache();
        cache.Set(null!, cachedPc);

        this.fetcherMock.Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(
            failure ? FetchResult.Failure(fetchedPc, RefreshErrorCode.HttpRequestFailure, "network error") : FetchResult.NotModified(fetchedPc));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime);
        var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(cache, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: true,
            hooks: hooks);

        // Act

        bool clientReadyCalled;
        ProjectConfig actualPc;
        using (service)
        {
            actualPc = isAsync ? await service.GetConfigAsync() : service.GetConfig();

            // Allow some time for other initalization callbacks to execute.
            using var cts = new CancellationTokenSource();
            var clientReadyTask = Task.Run(async () => await clientReadyTcs.Task);
            var task = await Task.WhenAny(clientReadyTask, Task.Delay(maxInitWaitTime, cts.Token));
            cts.Cancel();
            clientReadyCalled = task == clientReadyTask && task.Status == TaskStatus.RanToCompletion;

            await Task.Yield();

            // Wait for the hook event handlers to execute (as that might not happen if the service got disposed immediately).
            SpinWait.SpinUntil(() => configFetchedEvents.TryPeek(out _), TimeSpan.FromSeconds(1));
        }

        GC.KeepAlive(hooks);

        // Assert

        Assert.AreEqual(fetchedPc, actualPc);

        this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsTrue(clientReadyCalled);

        Assert.IsTrue(configFetchedEvents.Count > 0);
        Assert.IsTrue(configFetchedEvents.TryDequeue(out var configFetchedEvent));
        Assert.IsFalse(configFetchedEvent.IsInitiatedByUser);
        Assert.AreEqual(failure, !configFetchedEvent.Result.IsSuccess);
        Assert.AreEqual(failure ? RefreshErrorCode.HttpRequestFailure : RefreshErrorCode.None, configFetchedEvent.Result.ErrorCode);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task LazyLoadConfigService_GetConfig_ReturnsCachedConfigWhenCachedConfigIsNotExpired(bool isAsync)
    {
        // Arrange 

        var cacheTimeToLive = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateUpToDatePc(timeStamp, cacheTimeToLive);
        var fetchedPc = CreateFreshPc(timeStamp);

        var hooks = new Hooks();

        var clientReadyEventCount = 0;
        hooks.ClientReady += (s, e) => Interlocked.Increment(ref clientReadyEventCount);

        var configFetchedEventCount = 0;
        hooks.ConfigFetched += (s, e) => Interlocked.Increment(ref configFetchedEventCount);

        var cache = new InMemoryConfigCache();
        cache.Set(null!, cachedPc);

        if (isAsync)
        {
            this.fetcherMock.Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.Success(fetchedPc));
        }
        else
        {
            this.fetcherMock.Setup(m => m.Fetch(cachedPc)).Returns(FetchResult.Success(fetchedPc));
        }

        var config = PollingModes.LazyLoad(cacheTimeToLive);
        var service = new LazyLoadConfigService(this.fetcherMock.Object,
            new CacheParameters(cache, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            config.CacheTimeToLive,
            hooks: hooks);

        // Act

        ProjectConfig actualPc;
        using (service)
        {
            actualPc = isAsync ? await service.GetConfigAsync() : service.GetConfig();
        }

        GC.KeepAlive(hooks);

        // Assert

        Assert.AreEqual(cachedPc, actualPc);

        if (isAsync)
        {
            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Never);
        }
        else
        {
            this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Never);
        }

        Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));

        Assert.AreEqual(0, Volatile.Read(ref configFetchedEventCount));
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task LazyLoadConfigService_GetConfig_FetchesConfigWhenCachedConfigIsExpired(bool isAsync)
    {
        // Arrange 

        var cacheTimeToLive = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateExpiredPc(timeStamp, cacheTimeToLive);
        var fetchedPc = CreateFreshPc(timeStamp);

        var hooks = new Hooks();

        var clientReadyEventCount = 0;
        hooks.ClientReady += (s, e) => Interlocked.Increment(ref clientReadyEventCount);

        var configFetchedEvents = new ConcurrentQueue<ConfigFetchedEventArgs>();
        hooks.ConfigFetched += (s, e) => configFetchedEvents.Enqueue(e);

        var cache = new InMemoryConfigCache();
        cache.Set(null!, cachedPc);

        if (isAsync)
        {
            this.fetcherMock.Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.Success(fetchedPc));
        }
        else
        {
            this.fetcherMock.Setup(m => m.Fetch(cachedPc)).Returns(FetchResult.Success(fetchedPc));
        }

        var config = PollingModes.LazyLoad(cacheTimeToLive);
        var service = new LazyLoadConfigService(this.fetcherMock.Object,
            new CacheParameters(cache, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            config.CacheTimeToLive,
            hooks: hooks);

        // Act

        ProjectConfig actualPc;
        using (service)
        {
            actualPc = isAsync ? await service.GetConfigAsync() : service.GetConfig();
        }

        GC.KeepAlive(hooks);

        // Assert

        Assert.AreEqual(fetchedPc, actualPc);

        if (isAsync)
        {
            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Once);
        }

        Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));

        Assert.IsTrue(configFetchedEvents.Count > 0);
        Assert.IsTrue(configFetchedEvents.TryDequeue(out var configFetchedEvent));
        Assert.IsFalse(configFetchedEvent.IsInitiatedByUser);
        Assert.IsTrue(configFetchedEvent.Result.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.None, configFetchedEvent.Result.ErrorCode);
    }

    [DataTestMethod]
    [DataRow(nameof(PollingModes.AutoPoll))]
    [DataRow(nameof(PollingModes.LazyLoad))]
    [DataRow(nameof(PollingModes.ManualPoll))]
    public async Task GetInMemoryConfig_ImmediatelyReturnsEmptyConfig_WhenExternalCacheIsTrulyAsynchronous(string pollingMode)
    {
        // Arrange

        var pollInterval = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateUpToDatePc(timeStamp, pollInterval);
        var cachedPcSerialized = ProjectConfig.Serialize(cachedPc);

        var delay = TimeSpan.FromSeconds(1);

        var externalCache = new Mock<IConfigCatCache>();
        externalCache
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken _) => { await Task.Delay(delay); return cachedPcSerialized; });

        var logger = this.loggerMock.Object.AsWrapper();

        var service = CreateConfigService(pollingMode, pollInterval, maxInitWaitTime: TimeSpan.Zero, cacheTimeToLive: pollInterval,
            new CacheParameters(new ExternalConfigCache(externalCache.Object, logger), cacheKey: null!),
            this.fetcherMock.Object, logger);

        using var _ = service as IDisposable;

        // Act

        var inMemoryConfig = service.GetInMemoryConfig();

        await Task.Delay(delay + delay);

        var inMemoryConfig2 = service.GetInMemoryConfig();

        // Assert

        Assert.IsTrue(inMemoryConfig.IsEmpty);

        Assert.IsFalse(inMemoryConfig2.IsEmpty);
        Assert.AreEqual(cachedPcSerialized, ProjectConfig.Serialize(inMemoryConfig2));
    }

    [DataTestMethod]
    [DataRow(nameof(PollingModes.AutoPoll))]
    [DataRow(nameof(PollingModes.LazyLoad))]
    [DataRow(nameof(PollingModes.ManualPoll))]
    public void GetInMemoryConfig_ImmediatelyReturnsCachedConfig_WhenExternalCacheIsNotTrulyAsynchronous(string pollingMode)
    {
        // Arrange

        var pollInterval = TimeSpan.FromSeconds(30);
        var timeStamp = ProjectConfig.GenerateTimeStamp();
        var cachedPc = CreateUpToDatePc(timeStamp, pollInterval);
        var cachedPcSerialized = ProjectConfig.Serialize(cachedPc);

        var externalCache = new Mock<IConfigCatCache>();
        externalCache
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPcSerialized);

        var logger = this.loggerMock.Object.AsWrapper();

        ProjectConfig.Serialize(cachedPc);

        var service = CreateConfigService(pollingMode, pollInterval, maxInitWaitTime: TimeSpan.Zero, cacheTimeToLive: pollInterval,
            new CacheParameters(new ExternalConfigCache(externalCache.Object, logger), cacheKey: null!),
            this.fetcherMock.Object, logger);

        using var _ = service as IDisposable;

        // Act

        var inMemoryConfig = service.GetInMemoryConfig();

        // Assert

        Assert.IsFalse(inMemoryConfig.IsEmpty);
        Assert.AreEqual(cachedPcSerialized, ProjectConfig.Serialize(inMemoryConfig));
    }

    private static IConfigService CreateConfigService(string pollingMode, TimeSpan pollInterval, TimeSpan maxInitWaitTime, TimeSpan cacheTimeToLive, CacheParameters cacheParams,
        IConfigFetcher configFetcher, LoggerWrapper logger)
    {
        return pollingMode switch
        {
            nameof(PollingModes.AutoPoll) => new AutoPollConfigService(
                PollingModes.AutoPoll(pollInterval, maxInitWaitTime),
                configFetcher,
                cacheParams,
                logger,
                startTimer: false),

            nameof(PollingModes.LazyLoad) => new LazyLoadConfigService(
                configFetcher,
                cacheParams,
                logger,
                cacheTimeToLive),

            nameof(PollingModes.ManualPoll) => new ManualPollConfigService(
                configFetcher,
                cacheParams,
                logger),

            _ => throw new InvalidOperationException(),
        };
    }
}
