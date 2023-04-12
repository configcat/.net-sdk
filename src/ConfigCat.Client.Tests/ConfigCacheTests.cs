using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestCategory(TestCategories.Integration)]
[TestClass]
[DoNotParallelize]
public class ConfigCacheTests
{
    private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";
    private static readonly HttpClientHandler SharedHandler = new();

    [TestMethod]
    public async Task ConfigCache_Override_AutoPoll_Works()
    {
        ProjectConfig cachedConfig = ProjectConfig.Empty;
        var configCacheMock = new Mock<IConfigCatCache>();

        configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>())).Callback<string, ProjectConfig>((key, config) =>
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

        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.AtLeastOnce);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task ConfigCache_Override_ManualPoll_Works()
    {
        ProjectConfig cachedConfig = ProjectConfig.Empty;
        var configCacheMock = new Mock<IConfigCatCache>();
        configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>())).Callback<string, ProjectConfig>((key, config) =>
        {
            cachedConfig = config;
        });

        configCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(() => cachedConfig);

        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Debug);
            options.PollingMode = PollingModes.ManualPoll;
            options.ConfigCache = configCacheMock.Object;
            options.HttpClientHandler = SharedHandler;
        });

        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Never);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Never);

        var actual = await client.GetValueAsync("stringDefaultCat", "N/A");

        Assert.AreEqual("N/A", actual);
        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Never);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);

        await client.ForceRefreshAsync();

        actual = await client.GetValueAsync("stringDefaultCat", "N/A");
        Assert.AreEqual("Cat", actual);
        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.Once);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.Exactly(3));
    }

    [TestMethod]
    public async Task ConfigCache_Override_LazyLoad_Works()
    {
        ProjectConfig cachedConfig = ProjectConfig.Empty;
        var configCacheMock = new Mock<IConfigCatCache>();
        configCacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>())).Callback<string, ProjectConfig>((key, config) =>
        {
            cachedConfig = config;
        });

        configCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(() => cachedConfig);

        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Debug);
            options.PollingMode = PollingModes.LazyLoad();
            options.ConfigCache = configCacheMock.Object;
            options.HttpClientHandler = SharedHandler;
        });

        var actual = await client.GetValueAsync("stringDefaultCat", "N/A");
        Assert.AreEqual("Cat", actual);

        configCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProjectConfig>()), Times.AtLeastOnce);
        configCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), CancellationToken.None), Times.AtLeastOnce);
    }
}
