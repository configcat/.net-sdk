using System;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestCategory(TestCategories.Integration)]
[TestClass]
[DoNotParallelize]
public class BaseUrlTests
{
    private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

    private readonly Uri workingBaseUrl = new("https://cdn.configcat.com");
    private readonly Uri notWorkingBaseUrl = new("https://thiswillnotwork.configcat.com");

    [TestMethod]
    public void BaseUrl_Override_AutoPoll_Works()
    {
        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.AutoPoll();
            options.BaseUrl = this.workingBaseUrl;
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        }))
        {
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);
        }

        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.AutoPoll();
            options.BaseUrl = this.notWorkingBaseUrl;
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        }))
        {
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }

    [TestMethod]
    public void BaseUrl_Override_ManualPoll_Works()
    {
        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.BaseUrl = this.workingBaseUrl;
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        }))
        {
            client.ForceRefresh();
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);
        }

        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.BaseUrl = this.notWorkingBaseUrl;
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        }))
        {
            client.ForceRefresh();
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }

    [TestMethod]
    public void BaseUrl_Override_LazyLoad_Works()
    {
        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.LazyLoad();
            options.BaseUrl = this.workingBaseUrl;
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        }))
        {
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);
        }

        using (var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.PollingMode = PollingModes.LazyLoad();
            options.BaseUrl = this.notWorkingBaseUrl;
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        }))
        {
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }
}
