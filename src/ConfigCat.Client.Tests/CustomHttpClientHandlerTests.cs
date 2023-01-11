using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CS0618 // Type or member is obsolete
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

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void AutoPoll_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation(bool useNewCreateApi)
    {
        // Arrange

        var client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.HttpClientHandler = this.httpClientHandler;
                options.PollingMode = PollingModes.AutoPoll();
                options.DataGovernance = DataGovernance.EuOnly;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithDataGovernance(DataGovernance.EuOnly)
            .WithAutoPoll()
            .WithHttpClientHandler(this.httpClientHandler)
            .Create();

        // Act

        var actual = client.GetValue("stringDefaultCat", "N/A");

        // Assert

        Assert.AreEqual("Cat", actual);
        Assert.IsTrue(this.httpClientHandler.SendAsyncInvokeCount > 0);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void ManualPoll_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation(bool useNewCreateApi)
    {
        // Arrange

        var client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.HttpClientHandler = this.httpClientHandler;
                options.PollingMode = PollingModes.ManualPoll;
                options.DataGovernance = DataGovernance.EuOnly;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithDataGovernance(DataGovernance.EuOnly)
            .WithManualPoll()
            .WithHttpClientHandler(this.httpClientHandler)
            .Create();

        // Act

        client.ForceRefresh();
        var actual = client.GetValue("stringDefaultCat", "N/A");

        // Assert

        Assert.AreEqual("Cat", actual);
        Assert.AreEqual(1, this.httpClientHandler.SendAsyncInvokeCount);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void LazyLoad_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation(bool useNewCreateApi)
    {
        // Arrange

        var client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.HttpClientHandler = this.httpClientHandler;
                options.PollingMode = PollingModes.LazyLoad();
                options.DataGovernance = DataGovernance.EuOnly;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithDataGovernance(DataGovernance.EuOnly)
            .WithLazyLoad()
            .WithHttpClientHandler(this.httpClientHandler)
            .Create();

        // Act

        var actual = client.GetValue("stringDefaultCat", "N/A");

        // Assert

        Assert.AreEqual("Cat", actual);
        Assert.AreEqual(1, this.httpClientHandler.SendAsyncInvokeCount);
    }
}
