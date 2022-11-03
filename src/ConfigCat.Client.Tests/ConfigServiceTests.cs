using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.ConfigService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class ConfigServiceTests
    {
        static readonly TimeSpan defaultExpire = TimeSpan.FromSeconds(30);

        Mock<IConfigFetcher> fetcherMock = new Mock<IConfigFetcher>(MockBehavior.Strict);
        Mock<IConfigCatCache> cacheMock = new Mock<IConfigCatCache>(MockBehavior.Strict);
        Mock<ILogger> loggerMock = new Mock<ILogger>(MockBehavior.Loose);

        ProjectConfig cachedPc = new ProjectConfig("CACHED", DateTime.UtcNow.Subtract(defaultExpire.Add(TimeSpan.FromSeconds(1))), "67890");
        ProjectConfig fetchedPc = new ProjectConfig("FETCHED", DateTime.UtcNow, "12345");

        [TestInitialize]
        public void TestInitialize()
        {
            fetcherMock.Reset();
            cacheMock.Reset();
        }

        [DoNotParallelize]
        [TestMethod]
        public async Task LazyLoadConfigService_GetConfigAsync_ReturnsExpiredContent_ShouldInvokeFetchAndCacheSet()
        {
            // Arrange

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.FromResult(0))
                .Verifiable();

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Returns(Task.FromResult(fetchedPc))
                .Verifiable();

            using var service = new LazyLoadConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                defaultExpire);

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

            var cachedPc = new ProjectConfig("{}", DateTime.UtcNow, "123");

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            using var service = new LazyLoadConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                defaultExpire);

            // Act

            var projectConfig = await service.GetConfigAsync();

            // Assert

            Assert.AreEqual(cachedPc, projectConfig);

            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Never);
        }

        [TestMethod]
        public async Task LazyLoadConfigService_RefreshConfigAsync_ShouldNotInvokeCacheGetAndFetchAndCacheSet()
        {
            // Arrange

            byte callOrder = 1;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc)
                .Callback(() => Assert.AreEqual(1, callOrder++))
                .Verifiable();

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Returns(Task.FromResult(fetchedPc))
                .Callback(() => Assert.AreEqual(2, callOrder++))
                .Verifiable();

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.FromResult(0))
                .Callback(() => Assert.AreEqual(3, callOrder))
                .Verifiable();

            using var service = new LazyLoadConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                defaultExpire);

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
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.FromResult(0));

            using var service = new LazyLoadConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                defaultExpire,
                hooks: hooks);

            // Act

            await service.RefreshConfigAsync();

            // Assert

            Assert.IsTrue(configChangedEvents.TryDequeue(out var configChangedEvent));
            Assert.AreSame(fetchedPc, configChangedEvent.NewConfig);
            Assert.AreEqual(0, configChangedEvents.Count);
        }

        [TestMethod]
        public async Task AutoPollConfigService_GetConfigAsync_WithoutTimerWithCachedConfig_ShouldInvokeCacheGet1xAndSetNeverFetchNever()
        {
            // Arrange            

            var localPc = cachedPc;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(localPc);

            this.fetcherMock
                .Setup(m => m.FetchAsync(localPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Callback(() => localPc = fetchedPc)
                .Returns(Task.FromResult(0));

            var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
            using var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                startTimer: false);

            // Act            

            await service.GetConfigAsync();

            // Assert

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc), Times.Never);
            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc), Times.Never);
        }

        [TestMethod]
        public async Task AutoPollConfigService_GetConfigAsync_WithTimer_ShouldInvokeFetchAndCacheSetAndCacheGet3x()
        {
            // Arrange            

            var wd = new ManualResetEventSlim(false);

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Callback(() => wd.Set())
                .Returns(Task.FromResult(0));

            var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(0));
            using var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                startTimer: true);

            // Act            

            wd.Wait(TimeSpan.FromMinutes(1));

            await service.GetConfigAsync();
            service.Dispose();
            // Assert

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc), Times.Once);
            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc), Times.Once);
        }

        [TestMethod]
        public async Task AutoPollConfigService_RefreshConfigAsync_ShouldOnceInvokeCacheGetAndFetchAndCacheSet()
        {
            // Arrange

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.FromResult(0));

            var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(0));
            using var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                startTimer: false);

            // Act

            await service.RefreshConfigAsync();

            // Assert

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc), Times.Once);
            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc), Times.Once);
        }

        [TestMethod]
        public async Task AutoPollConfigService_RefreshConfigAsync_ConfigChanged_ShouldRaiseEvent()
        {
            // Arrange

            var hooks = new Hooks();

            var configChangedEvents = new ConcurrentQueue<ConfigChangedEventArgs>();
            hooks.ConfigChanged += (s, e) => configChangedEvents.Enqueue(e);

            byte eventChanged = 0;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.FromResult(0));

            var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(0));
            using var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                startTimer: false,
                hooks: hooks);

#pragma warning disable CS0618 // Type or member is obsolete
            config.OnConfigurationChanged += (o, s) => { eventChanged++; };
#pragma warning restore CS0618 // Type or member is obsolete

            // Act

            await service.RefreshConfigAsync();

            // Assert

            Assert.AreEqual(1, eventChanged);

            Assert.IsTrue(configChangedEvents.TryDequeue(out var configChangedEvent));
            Assert.AreSame(fetchedPc, configChangedEvent.NewConfig);
            Assert.AreEqual(0, configChangedEvents.Count);
        }

        [TestMethod]
        public async Task AutoPollConfigService_Dispose_ShouldStopTimer()
        {
            // Arrange           

            long counter = 0;
            long e1, e2;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(cachedPc));

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Callback(() => Interlocked.Increment(ref counter))
                .Returns(Task.FromResult(cachedPc));

            var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(0.2d), TimeSpan.FromSeconds(0));
            using var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object, CacheKey = "" },
                loggerMock.Object.AsWrapper(),
                startTimer: false);

            // Act
            await Task.Delay(TimeSpan.FromSeconds(1));
            e1 = Interlocked.Read(ref counter);
            service.Dispose();

            // Assert

            await Task.Delay(TimeSpan.FromSeconds(1));
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
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Callback(() => Interlocked.Increment(ref counter))
                .Returns(Task.FromResult(cachedPc));

            var config = PollingModes.AutoPoll(TimeSpan.FromSeconds(0.2d), TimeSpan.FromSeconds(0));
            using var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                startTimer: false);

            // Act
            await Task.Delay(TimeSpan.FromSeconds(1));
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
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            using var service = new ManualPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                hooks: hooks);

            // Act

            var projectConfig = await service.GetConfigAsync();

            // Assert

            Assert.AreEqual(cachedPc, projectConfig);

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Never);

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
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc)
                .Callback(() => Assert.AreEqual(1, callOrder++));

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Returns(Task.FromResult(fetchedPc))
                .Callback(() => Assert.AreEqual(2, callOrder++));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Callback(() => Assert.AreEqual(3, callOrder++))
                .Returns(Task.FromResult(0));

            using var service = new ManualPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                hooks: hooks);
            // Act

            await service.RefreshConfigAsync();

            // Assert

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
            this.fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Once);

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
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.FetchAsync(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.FromResult(0));

            using var service = new ManualPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object.AsWrapper(),
                hooks: hooks);

            // Act

            await service.RefreshConfigAsync();

            // Assert

            Assert.IsTrue(configChangedEvents.TryDequeue(out var configChangedEvent));
            Assert.AreSame(fetchedPc, configChangedEvent.NewConfig);
            Assert.AreEqual(0, configChangedEvents.Count);
        }

        [TestMethod]
        public void ConfigService_InvokeDisposeManyTimes_ShouldInvokeFetcherDisposeExactlyOnce()
        {
            // Arrange

            var configFetcherMock = new Mock<IConfigFetcher>();
            configFetcherMock
                .Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>()))
                .Returns(Task.FromResult(ProjectConfig.Empty));

            var configFetcherMockDispose = configFetcherMock.As<IDisposable>();

            configFetcherMockDispose.Setup(m => m.Dispose());

            var configServiceMock = new Mock<ConfigServiceBase>(
                MockBehavior.Loose,
                new object[]
                {
                    configFetcherMock.Object,
                    new CacheParameters { ConfigCache = new InMemoryConfigCache() },
                    loggerMock.Object.AsWrapper(),
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
                .Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>()))
                .Returns(Task.FromResult(ProjectConfig.Empty));

            var configServiceMock = new Mock<ConfigServiceBase>(
                MockBehavior.Loose,
                new object[]
                {
                    configFetcherMock.Object,
                    new CacheParameters { ConfigCache = new InMemoryConfigCache() },
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

            var clientReadyTcs = new TaskCompletionSource<object>();
            hooks.ClientReady += (s, e) => clientReadyTcs.TrySetResult(default);

            var pollInterval = defaultExpire + defaultExpire;
            var maxInitWaitTime = defaultExpire;

            var cache = new InMemoryConfigCache();
            cache.Set(null, cachedPc);

            this.fetcherMock.Setup(m => m.FetchAsync(cachedPc)).ReturnsAsync(fetchedPc);

            var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime);
            var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cache },
                loggerMock.Object.AsWrapper(),
                startTimer: true,
                hooks: hooks);

            // Act

            bool clientReadyCalled = false;
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

            Assert.AreEqual(cachedPc, actualPc);

            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc), Times.Never);

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

            var clientReadyTcs = new TaskCompletionSource<object>();
            hooks.ClientReady += (s, e) => clientReadyTcs.TrySetResult(default);

            var pollInterval = defaultExpire + defaultExpire;
            var maxInitWaitTime = defaultExpire;

            var cache = new InMemoryConfigCache();
            cache.Set(null, cachedPc with { TimeStamp = cachedPc.TimeStamp - pollInterval });

            this.fetcherMock.Setup(m => m.FetchAsync(cachedPc)).ReturnsAsync(fetchedPc);

            var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime);
            var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cache },
                loggerMock.Object.AsWrapper(),
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

            Assert.AreEqual(fetchedPc, actualPc);

            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc), Times.Once);

            Assert.IsTrue(clientReadyCalled);
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task AutoPollConfigService_GetConfig_ReturnsExpiredConfigWhenCantRefreshWithinMaxInitWaitTime(bool isAsync)
        {
            // Arrange 

            var hooks = new Hooks();

            var clientReadyTcs = new TaskCompletionSource<object>();
            hooks.ClientReady += (s, e) => clientReadyTcs.TrySetResult(default);

            var pollInterval = TimeSpan.FromSeconds(5);
            var maxInitWaitTime = pollInterval + pollInterval;

            var cache = new InMemoryConfigCache();
            var cachedPc = this.cachedPc with { TimeStamp = DateTime.UtcNow - pollInterval - pollInterval };
            cache.Set(null, cachedPc);

            this.fetcherMock.Setup(m => m.FetchAsync(cachedPc)).ReturnsAsync(cachedPc);

            var config = PollingModes.AutoPoll(pollInterval, maxInitWaitTime);
            var service = new AutoPollConfigService(config,
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cache },
                loggerMock.Object.AsWrapper(),
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

            this.fetcherMock.Verify(m => m.FetchAsync(cachedPc), Times.AtLeast(2));

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

            var cacheTimeToLive = defaultExpire + defaultExpire;

            var cache = new InMemoryConfigCache();
            cache.Set(null, cachedPc);

            if (isAsync)
            {
                this.fetcherMock.Setup(m => m.FetchAsync(cachedPc)).ReturnsAsync(fetchedPc);
            }
            else
            {
                this.fetcherMock.Setup(m => m.Fetch(cachedPc)).Returns(fetchedPc);
            }

            var config = PollingModes.LazyLoad(cacheTimeToLive);
            var service = new LazyLoadConfigService(fetcherMock.Object,
                new CacheParameters { ConfigCache = cache },
                loggerMock.Object.AsWrapper(),
                config.CacheTimeToLive,
                hooks: hooks);

            // Act

            ProjectConfig actualPc;
            using (service)
            {
                actualPc = isAsync ? await service.GetConfigAsync() : service.GetConfig();
            }
            // Assert

            Assert.AreEqual(cachedPc, actualPc);

            if (isAsync)
            {
                this.fetcherMock.Verify(m => m.FetchAsync(cachedPc), Times.Never);
            }
            else
            {
                this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Never);
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

            var cacheTimeToLive = defaultExpire + defaultExpire;

            var cache = new InMemoryConfigCache();
            cache.Set(null, cachedPc with { TimeStamp = cachedPc.TimeStamp - cacheTimeToLive });

            if (isAsync)
            {
                this.fetcherMock.Setup(m => m.FetchAsync(cachedPc)).ReturnsAsync(fetchedPc);
            }
            else 
            {
                this.fetcherMock.Setup(m => m.Fetch(cachedPc)).Returns(fetchedPc);
            }

            var config = PollingModes.LazyLoad(cacheTimeToLive);
            var service = new LazyLoadConfigService(fetcherMock.Object,
                new CacheParameters { ConfigCache = cache },
                loggerMock.Object.AsWrapper(),
                config.CacheTimeToLive,
                hooks: hooks);

            // Act

            ProjectConfig actualPc;
            using (service)
            {
                actualPc = isAsync ? await service.GetConfigAsync() : service.GetConfig();
            }
            // Assert

            Assert.AreEqual(fetchedPc, actualPc);

            if (isAsync)
            {
                this.fetcherMock.Verify(m => m.FetchAsync(cachedPc), Times.Once);
            }
            else
            {
                this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Once);
            }

            Assert.AreEqual(1, Volatile.Read(ref clientReadyEventCount));
        }
    }
}