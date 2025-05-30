using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestCategory(TestCategories.Integration)]
[TestClass]
[DoNotParallelize]
public class BaseUrlTests
{
    private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

    private static readonly HttpClientHandler SharedHandler = new();

    private readonly Uri workingBaseUrl = new("https://cdn.configcat.com");
    private readonly Uri notWorkingBaseUrl = new("https://thiswillnotwork.configcat.com");

    [TestMethod]
    public async Task BaseUrl_Override_AutoPoll_Works()
    {
        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.AutoPoll();
            options.BaseUrl = this.workingBaseUrl;
            options.HttpClientHandler = SharedHandler;
        }))
        {
            var actual = await client.GetValueAsync("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);
        }

        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.AutoPoll();
            options.BaseUrl = this.notWorkingBaseUrl;
            options.HttpClientHandler = SharedHandler;
        }))
        {
            var actual = await client.GetValueAsync("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }

    [TestMethod]
    public async Task BaseUrl_Override_ManualPoll_Works()
    {
        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.BaseUrl = this.workingBaseUrl;
            options.HttpClientHandler = SharedHandler;
        }))
        {
            await client.ForceRefreshAsync();
            var actual = await client.GetValueAsync("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);
        }

        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.BaseUrl = this.notWorkingBaseUrl;
            options.HttpClientHandler = SharedHandler;
        }))
        {
            await client.ForceRefreshAsync();
            var actual = await client.GetValueAsync("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }

    [TestMethod]
    public async Task BaseUrl_Override_LazyLoad_Works()
    {
        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.LazyLoad();
            options.BaseUrl = this.workingBaseUrl;
            options.HttpClientHandler = SharedHandler;
        }))
        {
            var actual = await client.GetValueAsync("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);
        }

        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.LazyLoad();
            options.BaseUrl = this.notWorkingBaseUrl;
            options.HttpClientHandler = SharedHandler;
        }))
        {
            var actual = await client.GetValueAsync("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }
}
