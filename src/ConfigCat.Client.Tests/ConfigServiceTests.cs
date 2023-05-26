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
    private static readonly TimeSpan DefaultExpire = TimeSpan.FromSeconds(30);

    private readonly Mock<IConfigFetcher> fetcherMock = new(MockBehavior.Strict);
    private readonly Mock<ConfigCache> cacheMock = new(MockBehavior.Strict);
    private readonly Mock<IConfigCatLogger> loggerMock = new(MockBehavior.Loose);
    private readonly ProjectConfig cachedPc = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp() - DefaultExpire - TimeSpan.FromSeconds(1), httpETag: "67890");
    private readonly ProjectConfig fetchedPc = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "12345");

    [TestInitialize]
    public void TestInitialize()
    {
        this.fetcherMock.Reset();
        this.cacheMock.Reset();
    }

    [DoNotParallelize]
    [TestMethod]
    public async Task LazyLoadConfigService_GetConfigAsync_ReturnsExpiredContent_ShouldInvokeFetchAndCacheSet()
    {
        // Arrange

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc);

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask))
            .Verifiable();

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(this.fetchedPc))
            .Verifiable();

        using var service = new LazyLoadConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            DefaultExpire);

        // Act

        var projectConfig = await service.GetConfigAsync();

        // Assert

        Assert.AreEqual(this.fetchedPc, projectConfig);

        this.fetcherMock.VerifyAll();
        this.cacheMock.VerifyAll();
    }

    [TestMethod]
    public async Task LazyLoadConfigService_GetConfigAsync_ReturnsNotExpiredContent_ShouldNotInvokeFetchAndCacheSet()
    {
        // Arrange

        var cachedPc = ConfigHelper.FromString("{}", "123", ProjectConfig.GenerateTimeStamp());

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPc);

        using var service = new LazyLoadConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            DefaultExpire);

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

        byte callOrder = 1;

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc)
            .Callback(() => Assert.AreEqual(1, callOrder++))
            .Verifiable();

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(this.fetchedPc))
            .Callback(() => Assert.AreEqual(2, callOrder++))
            .Verifiable();

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask))
            .Callback(() => Assert.AreEqual(3, callOrder))
            .Verifiable();

        using var service = new LazyLoadConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            DefaultExpire);

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

        var hooks = new Hooks();

        var configChangedEvents = new ConcurrentQueue<ConfigChangedEventArgs>();
        hooks.ConfigChanged += (s, e) => configChangedEvents.Enqueue(e);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(this.fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask));

        using var service = new LazyLoadConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            DefaultExpire,
            hooks: hooks);

        // Act

        await service.RefreshConfigAsync();

        // Assert

        Assert.IsTrue(configChangedEvents.TryDequeue(out var configChangedEvent));
        Assert.AreSame(this.fetchedPc.Config, configChangedEvent.NewConfig);
        Assert.AreEqual(0, configChangedEvents.Count);
    }

    [TestMethod]
    public async Task AutoPollConfigService_GetConfigAsync_WithoutTimerWithCachedConfig_ShouldInvokeCacheGet1xAndSetNeverFetchNever()
    {
        // Arrange            

        var localPc = this.cachedPc;

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(localPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(localPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(this.fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()))
            .Callback(() => localPc = this.fetchedPc)
            .Returns(default(ValueTask));

        var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: false);

        // Act            

        await service.GetConfigAsync();

        // Assert

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()), Times.Never);
        this.fetcherMock.Verify(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task AutoPollConfigService_GetConfigAsync_WithTimer_ShouldInvokeFetchAndCacheSetAndCacheGet3x()
    {
        // Arrange            

        var wd = new ManualResetEventSlim(false);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(this.fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()))
            .Callback(() => wd.Set())
            .Returns(default(ValueTask));

        var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(0));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: true);

        // Act            

        wd.Wait(TimeSpan.FromMinutes(1));

        await service.GetConfigAsync();
        service.Dispose();
        // Assert

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()), Times.Once);
        this.fetcherMock.Verify(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AutoPollConfigService_RefreshConfigAsync_ShouldOnceInvokeCacheGetAndFetchAndCacheSet()
    {
        // Arrange

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(this.fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask));

        var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(0));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: false);

        // Act

        await service.RefreshConfigAsync();

        // Assert

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()), Times.Once);
        this.fetcherMock.Verify(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AutoPollConfigService_Dispose_ShouldStopTimer()
    {
        // Arrange           

        long counter = 0;
        long e1, e2;

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref counter))
            .ReturnsAsync(FetchResult.Success(this.cachedPc));

        var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: ""),
            this.loggerMock.Object.AsWrapper(),
            startTimer: false);

        // Act
        await Task.Delay(TimeSpan.FromSeconds(2));
        e1 = Interlocked.Read(ref counter);
        service.Dispose();

        // Assert

        await Task.Delay(TimeSpan.FromSeconds(2));
        e2 = Interlocked.Read(ref counter);
        Console.WriteLine(e2 - e1);
        Assert.IsTrue(e2 - e1 <= 1);
    }

    [TestMethod]
    public async Task AutoPollConfigService_WithoutTimer_InvokeDispose_ShouldDisposeService()
    {
        // Arrange           

        long counter = -1;
        long e1;

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref counter))
            .ReturnsAsync(FetchResult.Success(this.cachedPc));

        var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0));
        using var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: false);

        // Act
        await Task.Delay(TimeSpan.FromSeconds(2));
        e1 = Interlocked.Read(ref counter);
        service.Dispose();

        // Assert            
        Assert.AreEqual(-1, e1);
    }

    [TestMethod]
    public async Task ManualPollConfigService_GetConfigAsync_ShouldInvokeCacheGet()
    {
        // Arrange

        var hooks = new Hooks();

        var clientReadyEventCount = 0;
        hooks.ClientReady += (s, e) => Interlocked.Increment(ref clientReadyEventCount);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc);

        using var service = new ManualPollConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            hooks: hooks);

        // Act

        var projectConfig = await service.GetConfigAsync();

        // Assert

        Assert.AreEqual(this.cachedPc, projectConfig);

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));
    }

    [TestMethod]
    public async Task ManualPollConfigService_RefreshConfigAsync_ShouldInvokeCacheGet()
    {
        // Arrange

        var hooks = new Hooks();

        var clientReadyEventCount = 0;
        hooks.ClientReady += (s, e) => Interlocked.Increment(ref clientReadyEventCount);

        byte callOrder = 1;

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc)
            .Callback(() => Assert.AreEqual(1, callOrder++));

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(this.fetchedPc))
            .Callback(() => Assert.AreEqual(2, callOrder++));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()))
            .Callback(() => Assert.AreEqual(3, callOrder++))
            .Returns(default(ValueTask));

        using var service = new ManualPollConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            hooks: hooks);
        // Act

        await service.RefreshConfigAsync();

        // Assert

        this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));
    }

    [TestMethod]
    public async Task ManualPollConfigService_RefreshConfigAsync_ConfigChanged_ShouldRaiseEvent()
    {
        // Arrange

        var hooks = new Hooks();

        var configChangedEvents = new ConcurrentQueue<ConfigChangedEventArgs>();
        hooks.ConfigChanged += (s, e) => configChangedEvents.Enqueue(e);

        this.cacheMock
            .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.cachedPc);

        this.fetcherMock
            .Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FetchResult.Success(this.fetchedPc));

        this.cacheMock
            .Setup(m => m.SetAsync(It.IsAny<string>(), this.fetchedPc, It.IsAny<CancellationToken>()))
            .Returns(default(ValueTask));

        using var service = new ManualPollConfigService(
            this.fetcherMock.Object,
            new CacheParameters(this.cacheMock.Object, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            hooks: hooks);

        // Act

        await service.RefreshConfigAsync();

        // Assert

        Assert.IsTrue(configChangedEvents.TryDequeue(out var configChangedEvent));
        Assert.AreSame(this.fetchedPc.Config, configChangedEvent.NewConfig);
        Assert.AreEqual(0, configChangedEvents.Count);
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

        var hooks = new Hooks();

        var clientReadyTcs = new TaskCompletionSource<object?>();
        hooks.ClientReady += (s, e) => clientReadyTcs.TrySetResult(default);

        var pollInterval = DefaultExpire + DefaultExpire;
        var maxInitWaitTime = DefaultExpire;

        var cache = new InMemoryConfigCache();
        cache.Set(null!, this.cachedPc);

        this.fetcherMock.Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.Success(this.fetchedPc));

        var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime);
        var service = new AutoPollConfigService(config,
            this.fetcherMock.Object,
            new CacheParameters(cache, cacheKey: null!),
            this.loggerMock.Object.AsWrapper(),
            startTimer: true,
            hooks: hooks);

        // Act

        var clientReadyCalled = false;
        ProjectConfig actualPc;
        using (service)
        {
            if (waitForClientReady)
            {
                await service.WaitForInitializationAsync();

                // Allow some time for other initalization callbacks to execute.
                using var cts = new CancellationTokenSource();
                var task = await Task.WhenAny(clientReadyTcs.Task, Task.Delay(maxInitWaitTime, cts.Token));
                cts.Cancel();
                clientReadyCalled = task == clientReadyTcs.Task && task.Status == TaskStatus.RanToCompletion;
            }

            actualPc = isAsync ? await service.GetConfigAsync() : service.GetConfig();
        }

        // Assert

        Assert.AreEqual(this.cachedPc, actualPc);

        this.fetcherMock.Verify(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()), Times.Never);

        if (waitForClientReady)
        {
            Assert.IsTrue(clientReadyCalled);
        }
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task AutoPollConfigService_GetConfig_FetchesConfigWhenCachedConfigIsExpired(bool isAsync)
    {
        // Arrange 

        var hooks = new Hooks();

        var clientReadyTcs = new TaskCompletionSource<object?>();
        hooks.ClientReady += (s, e) => clientReadyTcs.TrySetResult(default);

        var pollInterval = DefaultExpire + DefaultExpire;
        var maxInitWaitTime = DefaultExpire;

        var cache = new InMemoryConfigCache();
        var cachedPc = this.cachedPc.With(timeStamp: this.cachedPc.TimeStamp - pollInterval);
        cache.Set(null!, cachedPc);

        this.fetcherMock.Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.Success(this.fetchedPc));

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
            var task = await Task.WhenAny(clientReadyTcs.Task, Task.Delay(maxInitWaitTime, cts.Token));
            cts.Cancel();
            clientReadyCalled = task == clientReadyTcs.Task && task.Status == TaskStatus.RanToCompletion;
        }

        // Assert

        Assert.AreEqual(this.fetchedPc, actualPc);

        this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Once);

        Assert.IsTrue(clientReadyCalled);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task AutoPollConfigService_GetConfig_ReturnsExpiredConfigWhenCantRefreshWithinMaxInitWaitTime(bool isAsync)
    {
        // Arrange 

        var hooks = new Hooks();

        var clientReadyTcs = new TaskCompletionSource<object?>();
        hooks.ClientReady += (s, e) => clientReadyTcs.TrySetResult(default);

        var pollInterval = TimeSpan.FromSeconds(5);
        var maxInitWaitTime = pollInterval + pollInterval;

        var cache = new InMemoryConfigCache();
        var cachedPc = this.cachedPc.With(timeStamp: ProjectConfig.GenerateTimeStamp() - pollInterval - pollInterval);
        cache.Set(null!, cachedPc);

        this.fetcherMock.Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.NotModified(cachedPc));

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
            var task = await Task.WhenAny(clientReadyTcs.Task, Task.Delay(maxInitWaitTime, cts.Token));
            cts.Cancel();
            clientReadyCalled = task == clientReadyTcs.Task && task.Status == TaskStatus.RanToCompletion;
        }

        // Assert

        Assert.AreEqual(cachedPc, actualPc);

        this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.AtLeast(2));

        Assert.IsTrue(clientReadyCalled);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task LazyLoadConfigService_GetConfig_ReturnsCachedConfigWhenCachedConfigIsNotExpired(bool isAsync)
    {
        // Arrange 

        var hooks = new Hooks();

        var clientReadyEventCount = 0;
        hooks.ClientReady += (s, e) => Interlocked.Increment(ref clientReadyEventCount);

        var cacheTimeToLive = DefaultExpire + DefaultExpire;

        var cache = new InMemoryConfigCache();
        cache.Set(null!, this.cachedPc);

        if (isAsync)
        {
            this.fetcherMock.Setup(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.Success(this.fetchedPc));
        }
        else
        {
            this.fetcherMock.Setup(m => m.Fetch(this.cachedPc)).Returns(FetchResult.Success(this.fetchedPc));
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
        // Assert

        Assert.AreEqual(this.cachedPc, actualPc);

        if (isAsync)
        {
            this.fetcherMock.Verify(m => m.FetchAsync(this.cachedPc, It.IsAny<CancellationToken>()), Times.Never);
        }
        else
        {
            this.fetcherMock.Verify(m => m.Fetch(this.cachedPc), Times.Never);
        }

        Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public async Task LazyLoadConfigService_GetConfig_FetchesConfigWhenCachedConfigIsExpired(bool isAsync)
    {
        // Arrange 

        var hooks = new Hooks();

        var clientReadyEventCount = 0;
        hooks.ClientReady += (s, e) => Interlocked.Increment(ref clientReadyEventCount);

        var cacheTimeToLive = DefaultExpire + DefaultExpire;

        var cache = new InMemoryConfigCache();
        var cachedPc = this.cachedPc.With(timeStamp: this.cachedPc.TimeStamp - cacheTimeToLive);
        cache.Set(null!, cachedPc);

        if (isAsync)
        {
            this.fetcherMock.Setup(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>())).ReturnsAsync(FetchResult.Success(this.fetchedPc));
        }
        else
        {
            this.fetcherMock.Setup(m => m.Fetch(cachedPc)).Returns(FetchResult.Success(this.fetchedPc));
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
        // Assert

        Assert.AreEqual(this.fetchedPc, actualPc);

        if (isAsync)
        {
            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc, It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Once);
        }

        Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));
    }
}
