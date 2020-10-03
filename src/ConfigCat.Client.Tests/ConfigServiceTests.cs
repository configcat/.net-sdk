using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.ConfigService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests
{
    [DoNotParallelize]
    [TestClass]
    public class ConfigServiceTests
    {
        static readonly TimeSpan defaultExpire = TimeSpan.FromSeconds(30);

        Mock<IConfigFetcher> fetcherMock = new Mock<IConfigFetcher>(MockBehavior.Strict);
        Mock<IConfigCache> cacheMock = new Mock<IConfigCache>(MockBehavior.Strict);
        Mock<ILogger> loggerMock = new Mock<ILogger>(MockBehavior.Loose);

        ProjectConfig cachedPc = new ProjectConfig("CACHED", DateTime.UtcNow.Add(-defaultExpire), "67890");
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
                .Returns(Task.CompletedTask)
                .Verifiable();

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc))
                .Verifiable();

            var service = new LazyLoadConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object,
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

            var cachedPc = new ProjectConfig(null, DateTime.UtcNow, null);

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            var service = new LazyLoadConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object,
                defaultExpire);

            // Act

            var projectConfig = await service.GetConfigAsync();

            // Assert

            Assert.AreEqual(cachedPc, projectConfig);

            this.fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
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
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc))
                .Callback(() => Assert.AreEqual(2, callOrder++))
                .Verifiable();

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.CompletedTask)
                .Callback(() => Assert.AreEqual(3, callOrder))
                .Verifiable();

            var service = new LazyLoadConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object,
                defaultExpire);

            // Act

            await service.RefreshConfigAsync();

            // Assert

            this.fetcherMock.VerifyAll();
            this.cacheMock.VerifyAll();
        }

        [TestMethod]
        public async Task AutoPollConfigService_GetConfigAsync_WithoutTimer_ShouldInvokeFetchAndCacheSetAndCacheGet2x()
        {
            // Arrange            

            var localPc = cachedPc;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(localPc);

            this.fetcherMock
                .Setup(m => m.Fetch(localPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Callback(() => localPc = fetchedPc)
                .Returns(Task.CompletedTask);

            var service = new AutoPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1),
                loggerMock.Object,
                false);

            // Act            

            await service.GetConfigAsync();

            // Assert

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Exactly(2));
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc), Times.Once);
            this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Once);
        }

        [TestMethod]
        public async Task AutoPollConfigService_GetConfigAsync_WithTimer_ShouldInvokeFetchAndCacheSetAndCacheGet2x()
        {
            // Arrange            

            var wd = new ManualResetEventSlim(false);

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Callback(() => wd.Set())
                .Returns(Task.FromResult(0));

            var service = new AutoPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                TimeSpan.FromMinutes(1),
                TimeSpan.Zero,
                loggerMock.Object,
                true);

            // Act            

            wd.Wait(TimeSpan.FromMinutes(1));

            await service.GetConfigAsync();
            service.Dispose();
            // Assert

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Exactly(2));
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc), Times.Once);
            this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Once);
        }

        [TestMethod]
        public async Task AutoPollConfigService_RefreshConfigAsync_ShouldOnceInvokeCacheGetAndFetchAndCacheSet()
        {
            // Arrange

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.CompletedTask);

            var service = new AutoPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                TimeSpan.FromMinutes(1),
                TimeSpan.Zero,
                loggerMock.Object,
                false);

            // Act

            await service.RefreshConfigAsync();

            // Assert

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), fetchedPc), Times.Once);
            this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Once);
        }

        [TestMethod]
        public async Task AutoPollConfigService_RefreshConfigAsync_ConfigChanged_ShouldRaiseEvent()
        {
            // Arrange

            byte eventChanged = 0;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Returns(Task.CompletedTask);

            var service = new AutoPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                TimeSpan.FromMinutes(1),
                TimeSpan.Zero,
                loggerMock.Object,
                false);

            service.OnConfigurationChanged += (o, s) => { eventChanged++; };

            // Act

            await service.RefreshConfigAsync();

            // Assert

            Assert.AreEqual(1, eventChanged);
        }
        
        [TestMethod]
        public void AutoPollConfigService_Dispose_ShouldStopTimer()
        {
            // Arrange           

            long counter = 0;
            long e1, e2;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(cachedPc));

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Callback(() => Interlocked.Increment(ref counter))
                .Returns(Task.FromResult(cachedPc));

            var service = new AutoPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object, CacheKey = ""},
                TimeSpan.FromSeconds(0.2d),
                TimeSpan.Zero,
                loggerMock.Object,
                false);

            // Act
            Thread.Sleep(TimeSpan.FromSeconds(1));
            e1 = Interlocked.Read(ref counter);
            service.Dispose();

            // Assert

            Thread.Sleep(TimeSpan.FromSeconds(2));
            e2 = Interlocked.Read(ref counter);
            Console.WriteLine(e2 - e1);
            Assert.IsTrue(e2 - e1 <= 1);
        }

        [TestMethod]
        public void AutoPollConfigService_WithoutTimer_InvokeDispose_ShouldDisposeService()
        {
            // Arrange           

            long counter = -1;
            long e1;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Callback(() => Interlocked.Increment(ref counter))
                .Returns(Task.FromResult(cachedPc));

            var service = new AutoPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                TimeSpan.FromSeconds(0.2d),
                TimeSpan.Zero,
                loggerMock.Object,
                false);

            // Act
            Thread.Sleep(TimeSpan.FromSeconds(1));
            e1 = Interlocked.Read(ref counter);
            service.Dispose();

            // Assert            
            Assert.AreEqual(-1, e1);
        }

        [TestMethod]
        public async Task ManualPollConfigService_GetConfigAsync_ShouldInvokeCacheGet()
        {
            // Arrange

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc);

            var service = new ManualPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object);

            // Act

            var projectConfig = await service.GetConfigAsync();

            // Assert

            Assert.AreEqual(cachedPc, projectConfig);

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
            this.fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Never);
        }

        [TestMethod]
        public async Task ManualPollConfigService_RefreshConfigAsync_ShouldInvokeCacheGet()
        {
            // Arrange

            byte callOrder = 1;

            this.cacheMock
                .Setup(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(cachedPc)
                .Callback(() => Assert.AreEqual(1, callOrder++));

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc))
                .Callback(() => Assert.AreEqual(2, callOrder++));

            this.cacheMock
                .Setup(m => m.SetAsync(It.IsAny<string>(), fetchedPc))
                .Callback(() => Assert.AreEqual(3, callOrder++))
                .Returns(Task.CompletedTask);

            var service = new ManualPollConfigService(
                fetcherMock.Object,
                new CacheParameters { ConfigCache = cacheMock.Object },
                loggerMock.Object);

            // Act

            await service.RefreshConfigAsync();

            // Assert

            this.cacheMock.Verify(m => m.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
            this.fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Once);
            this.cacheMock.Verify(m => m.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Once);
        }

        [TestMethod]
        public void ConfigService_InvokeDisposeManyTimes_ShouldInvokeFetcherDisposeExactlyOnce()
        {
            // Arrange

            var configFetcherMock = new Mock<IConfigFetcher>();
            configFetcherMock
                .Setup(m => m.Fetch(It.IsAny<ProjectConfig>()))
                .Returns(Task.FromResult(ProjectConfig.Empty));

            var configFetcherMockDispose = configFetcherMock.As<IDisposable>();

            configFetcherMockDispose.Setup(m => m.Dispose());

            var configServiceMock = new Mock<ConfigServiceBase>(
                MockBehavior.Loose,
                configFetcherMock.Object,
                new CacheParameters { ConfigCache = new InMemoryConfigCache() },
                loggerMock.Object)
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
                .Setup(m => m.Fetch(It.IsAny<ProjectConfig>()))
                .Returns(Task.FromResult(ProjectConfig.Empty));

            var configServiceMock = new Mock<ConfigServiceBase>(
                MockBehavior.Loose,
                configFetcherMock.Object,
                new CacheParameters { ConfigCache = new InMemoryConfigCache() },
                new MyCounterLogger())
            {
                CallBase = true
            };

            var configService = configServiceMock.Object as IDisposable;

            // Act

            configService.Dispose();
        }
    }
}


