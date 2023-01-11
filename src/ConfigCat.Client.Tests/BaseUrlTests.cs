using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CS0618 // Type or member is obsolete
namespace ConfigCat.Client.Tests;

[TestCategory(TestCategories.Integration)]
[TestClass]
public class BaseUrlTests
{
    private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

    private static readonly HttpClientHandler SharedHandler = new();

    private readonly Uri workingBaseUrl = new("https://cdn.configcat.com");
    private readonly Uri notWorkingBaseUrl = new("https://thiswillnotwork.configcat.com");

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void BaseUrl_Override_AutoPoll_Works(bool useNewCreateApi)
    {
        var client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.PollingMode = PollingModes.AutoPoll();
                options.BaseUrl = this.workingBaseUrl;
                options.HttpClientHandler = SharedHandler;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithAutoPoll()
            .WithBaseUrl(this.workingBaseUrl)
            .WithHttpClientHandler(SharedHandler)
            .Create();

        var actual = client.GetValue("stringDefaultCat", "N/A");
        Assert.AreEqual("Cat", actual);

        client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.PollingMode = PollingModes.AutoPoll();
                options.BaseUrl = this.notWorkingBaseUrl;
                options.HttpClientHandler = SharedHandler;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithAutoPoll()
            .WithBaseUrl(this.notWorkingBaseUrl)
            .WithHttpClientHandler(SharedHandler)
            .Create();

        actual = client.GetValue("stringDefaultCat", "N/A");
        Assert.AreEqual("N/A", actual);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void BaseUrl_Override_ManualPoll_Works(bool useNewCreateApi)
    {
        var client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.PollingMode = PollingModes.ManualPoll;
                options.BaseUrl = this.workingBaseUrl;
                options.HttpClientHandler = SharedHandler;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithManualPoll()
            .WithBaseUrl(this.workingBaseUrl)
            .WithHttpClientHandler(SharedHandler)
            .Create();
        client.ForceRefresh();
        var actual = client.GetValue("stringDefaultCat", "N/A");
        Assert.AreEqual("Cat", actual);

        client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.PollingMode = PollingModes.ManualPoll;
                options.BaseUrl = this.notWorkingBaseUrl;
                options.HttpClientHandler = SharedHandler;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithManualPoll()
            .WithBaseUrl(this.notWorkingBaseUrl)
            .WithHttpClientHandler(SharedHandler)
            .Create();
        client.ForceRefresh();
        actual = client.GetValue("stringDefaultCat", "N/A");
        Assert.AreEqual("N/A", actual);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void BaseUrl_Override_LazyLoad_Works(bool useNewCreateApi)
    {
        var client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.PollingMode = PollingModes.LazyLoad();
                options.BaseUrl = this.workingBaseUrl;
                options.HttpClientHandler = SharedHandler;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithLazyLoad()
            .WithBaseUrl(this.workingBaseUrl)
            .WithHttpClientHandler(SharedHandler)
            .Create();

        var actual = client.GetValue("stringDefaultCat", "N/A");
        Assert.AreEqual("Cat", actual);

        client = useNewCreateApi
            ? new ConfigCatClient(options =>
            {
                options.SdkKey = SDKKEY;
                options.PollingMode = PollingModes.LazyLoad();
                options.BaseUrl = this.notWorkingBaseUrl;
                options.HttpClientHandler = SharedHandler;
            })
            : ConfigCatClientBuilder.Initialize(SDKKEY)
            .WithLazyLoad()
            .WithBaseUrl(this.notWorkingBaseUrl)
            .WithHttpClientHandler(SharedHandler)
            .Create();

        actual = client.GetValue("stringDefaultCat", "N/A");
        Assert.AreEqual("N/A", actual);
    }
}
