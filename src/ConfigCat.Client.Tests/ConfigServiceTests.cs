using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class ConfigServiceTests
    {
        static readonly TimeSpan defaultExpire = TimeSpan.FromSeconds(30);

        Mock<IConfigFetcher> fetcherMock = new Mock<IConfigFetcher>(MockBehavior.Strict);
        Mock<IConfigCache> cacheMock = new Mock<IConfigCache>(MockBehavior.Strict);
        Mock<ILoggerFactory> logFactoryMock = new Mock<ILoggerFactory>(MockBehavior.Strict);

        ProjectConfig cachedPc = new ProjectConfig("CACHED", DateTime.UtcNow.Add(-defaultExpire), "67890");
        ProjectConfig fetchedPc = new ProjectConfig("FETCHED", DateTime.UtcNow, "12345");

        [TestInitialize]
        public void TestInitialize()
        {
            fetcherMock.Reset();
            cacheMock.Reset();
            logFactoryMock.Reset();

            logFactoryMock
                .Setup(m => m.GetLogger(It.IsAny<string>()))
                .Returns(new NullLogger());
        }

        [TestMethod]
        public async Task LazyLoadConfigService_GetConfigAsync_ReturnsExpiredContent_ShouldInvokeFetchAndCacheSet()
        {
            // Arrange

            this.cacheMock
                .Setup(m => m.Get())
                .Returns(cachedPc);

            this.cacheMock
                .Setup(m => m.Set(fetchedPc))
                .Verifiable();

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc))
                .Verifiable();

            var service = new LazyLoadConfigService(
                fetcherMock.Object,
                cacheMock.Object,
                logFactoryMock.Object,
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
                .Setup(m => m.Get())
                .Returns(cachedPc);

            var service = new LazyLoadConfigService(
                fetcherMock.Object,
                cacheMock.Object,
                logFactoryMock.Object,
                defaultExpire);

            // Act

            var projectConfig = await service.GetConfigAsync();

            // Assert

            Assert.AreEqual(cachedPc, projectConfig);

            this.fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
            this.cacheMock.Verify(m => m.Set(It.IsAny<ProjectConfig>()), Times.Never);
        }

        [TestMethod]
        public async Task LazyLoadConfigService_RefreshConfigAsync_ShouldNotInvokeCacheGetAndFetchAndCacheSet()
        {
            // Arrange

            byte callOrder = 1;

            this.cacheMock
                .Setup(m => m.Get())
                .Returns(cachedPc)
                .Callback(() => Assert.AreEqual(1, callOrder++))
                .Verifiable();

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc))
                .Callback(() => Assert.AreEqual(2, callOrder++))
                .Verifiable();

            this.cacheMock
                .Setup(m => m.Set(fetchedPc))
                .Callback(() => Assert.AreEqual(3, callOrder))
                .Verifiable();

            var service = new LazyLoadConfigService(
                fetcherMock.Object,
                cacheMock.Object,
                logFactoryMock.Object,
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
                .Setup(m => m.Get())
                .Returns(localPc);

            this.fetcherMock
                .Setup(m => m.Fetch(localPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.Set(fetchedPc))
                .Callback(() => localPc = fetchedPc);
                
            var service = new AutoPollConfigService(
                fetcherMock.Object,
                cacheMock.Object,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1),
                logFactoryMock.Object,
                false);

            // Act            

            await service.GetConfigAsync();

            // Assert

            this.cacheMock.Verify(m => m.Get(), Times.Exactly(2));
            this.cacheMock.Verify(m => m.Set(fetchedPc), Times.Once);
            this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Once);            
        }

        [TestMethod]
        public async Task AutoPollConfigService_GetConfigAsync_WithTimer_ShouldInvokeFetchAndCacheSetAndCacheGet2x()
        {
            // Arrange            
            
            var wd = new ManualResetEventSlim(false);

            this.cacheMock
                .Setup(m => m.Get())
                .Returns(cachedPc);

            this.fetcherMock
                .Setup(m => m.Fetch(cachedPc))
                .Returns(Task.FromResult(fetchedPc));

            this.cacheMock
                .Setup(m => m.Set(fetchedPc))
                .Callback(() => wd.Set());

            var service = new AutoPollConfigService(
                fetcherMock.Object,
                cacheMock.Object,
                TimeSpan.FromMinutes(1),
                TimeSpan.Zero,
                logFactoryMock.Object,
                true);

            // Act            

            wd.Wait(TimeSpan.FromMinutes(1));

            await service.GetConfigAsync();

            // Assert

            this.cacheMock.Verify(m => m.Get(), Times.Exactly(2));
            this.cacheMock.Verify(m => m.Set(fetchedPc), Times.Once);
            this.fetcherMock.Verify(m => m.Fetch(cachedPc), Times.Once);
        }
    }
}


