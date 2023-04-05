using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestCategory(TestCategories.Integration)]
[TestClass]
[DoNotParallelize]
public class CustomHttpClientHandlerTests
{
    private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";
    private readonly RequestCounterHttpClientHandler httpClientHandler = new();

    [TestInitialize]
    public void TestInit()
    {
        this.httpClientHandler.Reset();
    }

    [TestMethod]
    public void AutoPoll_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation()
    {
        // Arrange

        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.HttpClientHandler = this.httpClientHandler;
            options.PollingMode = PollingModes.AutoPoll();
            options.DataGovernance = DataGovernance.EuOnly;
        });

        // Act

        var actual = client.GetValue("stringDefaultCat", "N/A");

        // Assert

        Assert.AreEqual("Cat", actual);
        Assert.IsTrue(this.httpClientHandler.SendAsyncInvokeCount > 0);
    }

    [TestMethod]
    public void ManualPoll_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation()
    {
        // Arrange

        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.HttpClientHandler = this.httpClientHandler;
            options.PollingMode = PollingModes.ManualPoll;
            options.DataGovernance = DataGovernance.EuOnly;
        });

        // Act

        client.ForceRefresh();
        var actual = client.GetValue("stringDefaultCat", "N/A");

        // Assert

        Assert.AreEqual("Cat", actual);
        Assert.AreEqual(1, this.httpClientHandler.SendAsyncInvokeCount);
    }

    [TestMethod]
    public void LazyLoad_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation()
    {
        // Arrange

        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.HttpClientHandler = this.httpClientHandler;
            options.PollingMode = PollingModes.LazyLoad();
            options.DataGovernance = DataGovernance.EuOnly;
        });

        // Act

        var actual = client.GetValue("stringDefaultCat", "N/A");

        // Assert

        Assert.AreEqual("Cat", actual);
        Assert.AreEqual(1, this.httpClientHandler.SendAsyncInvokeCount);
    }
}
