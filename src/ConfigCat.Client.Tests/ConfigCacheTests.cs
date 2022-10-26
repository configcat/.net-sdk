using System.Net.Http;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#pragma warning disable CS0618 // Type or member is obsolete
namespace ConfigCat.Client.Tests
{
    [TestCategory(TestCategories.Integration)]
    [TestClass]
    [DoNotParallelize]
    public class ConfigCacheTests
    {
        private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";
        private static readonly HttpClientHandler sharedHandler = new HttpClientHandler();

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void ConfigCache_Override_AutoPoll_Works(bool useNewCreateApi)
        {
            ProjectConfig cachedConfig = ProjectConfig.Empty;
            Mock<IConfigCache> configCacheMock = new Mock<IConfigCache>();
            
            configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>())).Callback<string, ProjectConfig>((key, config) =>
            {
                cachedConfig = config;
            });

            configCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => cachedConfig);
            
            var client = useNewCreateApi 
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.Logger = new ConsoleLogger(LogLevel.Debug);
                    options.PollingMode = PollingModes.AutoPoll();
                    options.ConfigCache = configCacheMock.Object;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder
                    .Initialize(SDKKEY)
                    .WithLogger(new ConsoleLogger(LogLevel.Debug))
                    .WithAutoPoll()
                    .WithConfigCache(configCacheMock.Object)
                    .WithHttpClientHandler(sharedHandler)
                    .Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");

            Assert.AreEqual("Cat", actual);

            configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.AtLeastOnce);
            configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.AtLeastOnce);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void ConfigCache_Override_ManualPoll_Works(bool useNewCreateApi)
        {
            ProjectConfig cachedConfig = ProjectConfig.Empty;
            Mock<IConfigCache> configCacheMock = new Mock<IConfigCache>();
            configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>())).Callback<string, ProjectConfig>((key, config) =>
            {
                cachedConfig = config;
            });

            configCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(() => cachedConfig);

            var client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.Logger = new ConsoleLogger(LogLevel.Debug);
                    options.PollingMode = PollingModes.ManualPoll;
                    options.ConfigCache = configCacheMock.Object;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                    .WithManualPoll()
                    .WithConfigCache(configCacheMock.Object)
                    .WithHttpClientHandler(sharedHandler)
                    .Create();

            configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Never);
            configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Never);

            var actual = client.GetValue("stringDefaultCat", "N/A");

            Assert.AreEqual("N/A", actual);
            configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Never);
            configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);

            client.ForceRefresh();

            actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);
            configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Once);
            configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Exactly(3));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void ConfigCache_Override_LazyLoad_Works(bool useNewCreateApi)
        {
            ProjectConfig cachedConfig = ProjectConfig.Empty;
            Mock<IConfigCache> configCacheMock = new Mock<IConfigCache>();
            configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>())).Callback<string, ProjectConfig>((key, config) =>
            {
                cachedConfig = config;
            });

            configCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(() => cachedConfig);

            var client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.Logger = new ConsoleLogger(LogLevel.Debug);
                    options.PollingMode = PollingModes.LazyLoad();
                    options.ConfigCache = configCacheMock.Object;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                    .WithLazyLoad()
                    .WithConfigCache(configCacheMock.Object)
                    .WithHttpClientHandler(sharedHandler)
                    .Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.AtLeastOnce);
            configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.AtLeastOnce);
        }
    }
}
