using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class ConfigCacheTests
    {
        private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

        [TestMethod]
        public void ConfigCache_Override_AutoPoll_Works()
        {
            ProjectConfig cachedConfig = ProjectConfig.Empty;
            Mock<IConfigCache> configCacheMock = new Mock<IConfigCache>();
            configCacheMock.Setup(c => c.Set(It.IsAny<ProjectConfig>())).Callback<ProjectConfig>((config) =>
            {
                cachedConfig = config;
            });

            configCacheMock.Setup(c => c.Get()).Returns(() => cachedConfig);

            var client = ConfigCatClientBuilder.Initialize(SDKKEY).WithAutoPoll().WithConfigCache(configCacheMock.Object).Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            configCacheMock.Verify(c => c.Set(It.IsAny<ProjectConfig>()), Times.AtLeastOnce);
            configCacheMock.Verify(c => c.Get(), Times.AtLeastOnce);
        }

        [TestMethod]
        public void ConfigCache_Override_ManualPoll_Works()
        {
            ProjectConfig cachedConfig = ProjectConfig.Empty;
            Mock<IConfigCache> configCacheMock = new Mock<IConfigCache>();
            configCacheMock.Setup(c => c.Set(It.IsAny<ProjectConfig>())).Callback<ProjectConfig>((config) =>
            {
                cachedConfig = config;
            });

            configCacheMock.Setup(c => c.Get()).Returns(() => cachedConfig);

            var client = ConfigCatClientBuilder.Initialize(SDKKEY).WithManualPoll().WithConfigCache(configCacheMock.Object).Create();

            configCacheMock.Verify(c => c.Set(It.IsAny<ProjectConfig>()), Times.Never);
            configCacheMock.Verify(c => c.Get(), Times.Never);

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
            configCacheMock.Verify(c => c.Set(It.IsAny<ProjectConfig>()), Times.Never);
            configCacheMock.Verify(c => c.Get(), Times.Once);

            client.ForceRefresh();

            actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);
            configCacheMock.Verify(c => c.Set(It.IsAny<ProjectConfig>()), Times.Once);
            configCacheMock.Verify(c => c.Get(), Times.Exactly(3));
        }

        [TestMethod]
        public void ConfigCache_Override_LazyLoad_Works()
        {
            ProjectConfig cachedConfig = ProjectConfig.Empty;
            Mock<IConfigCache> configCacheMock = new Mock<IConfigCache>();
            configCacheMock.Setup(c => c.Set(It.IsAny<ProjectConfig>())).Callback<ProjectConfig>((config) =>
            {
                cachedConfig = config;
            });

            configCacheMock.Setup(c => c.Get()).Returns(() => cachedConfig);

            var client = ConfigCatClientBuilder.Initialize(SDKKEY).WithLazyLoad().WithConfigCache(configCacheMock.Object).Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            configCacheMock.Verify(c => c.Set(It.IsAny<ProjectConfig>()), Times.AtLeastOnce);
            configCacheMock.Verify(c => c.Get(), Times.AtLeastOnce);
        }
    }
}
