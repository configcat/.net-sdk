using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CS0618 // Type or member is obsolete
namespace ConfigCat.Client.Tests
{
    [TestCategory(TestCategories.Integration)]
    [TestClass]
    public class BaseUrlTests
    {
        private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

        private readonly Uri workingBaseUrl = new Uri("https://cdn.configcat.com");
        private readonly Uri notWorkingBaseUrl = new Uri("https://thiswillnotwork.configcat.com");
        private readonly HttpClientHandler sharedHandler = new HttpClientHandler();

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
                    options.BaseUrl = workingBaseUrl;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithAutoPoll()
                .WithBaseUrl(workingBaseUrl)
                .WithHttpClientHandler(sharedHandler)
                .Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.AutoPoll();
                    options.BaseUrl = notWorkingBaseUrl;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithAutoPoll()
                .WithBaseUrl(notWorkingBaseUrl)
                .WithHttpClientHandler(sharedHandler)
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
                    options.BaseUrl = workingBaseUrl;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithManualPoll()
                .WithBaseUrl(workingBaseUrl)
                .WithHttpClientHandler(sharedHandler)
                .Create();
            client.ForceRefresh();
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.BaseUrl = notWorkingBaseUrl;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithManualPoll()
                .WithBaseUrl(notWorkingBaseUrl)
                .WithHttpClientHandler(sharedHandler)
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
                    options.BaseUrl = workingBaseUrl;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithLazyLoad()
                .WithBaseUrl(workingBaseUrl)
                .WithHttpClientHandler(sharedHandler)
                .Create();
            
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.LazyLoad();
                    options.BaseUrl = notWorkingBaseUrl;
                    options.HttpClientHandler = sharedHandler;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithLazyLoad()
                .WithBaseUrl(notWorkingBaseUrl)
                .WithHttpClientHandler(sharedHandler)
                .Create();

            actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }
}
