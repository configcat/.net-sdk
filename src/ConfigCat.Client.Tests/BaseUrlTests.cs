using System;
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

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void BaseUrl_Override_AutoPoll_Works(bool useNewCreateApi)
        {
            using var client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.AutoPoll();
                    options.BaseUrl = workingBaseUrl;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithAutoPoll()
                .WithBaseUrl(workingBaseUrl)
                .Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            using var newClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.AutoPoll();
                    options.BaseUrl = notWorkingBaseUrl;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithAutoPoll()
                .WithBaseUrl(notWorkingBaseUrl)
                .Create();

            actual = newClient.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void BaseUrl_Override_ManualPoll_Works(bool useNewCreateApi)
        {
            using var client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.BaseUrl = workingBaseUrl;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithManualPoll()
                .WithBaseUrl(workingBaseUrl)
                .Create();
            client.ForceRefresh();
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            using var newClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.BaseUrl = notWorkingBaseUrl;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithManualPoll()
                .WithBaseUrl(notWorkingBaseUrl)
                .Create();
            client.ForceRefresh();
            actual = newClient.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void BaseUrl_Override_LazyLoad_Works(bool useNewCreateApi)
        {
            using var client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.LazyLoad();
                    options.BaseUrl = workingBaseUrl;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithLazyLoad()
                .WithBaseUrl(workingBaseUrl)
                .Create();
            
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            using var newClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.LazyLoad();
                    options.BaseUrl = notWorkingBaseUrl;
                })
                : ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithLazyLoad()
                .WithBaseUrl(notWorkingBaseUrl)
                .Create();

            actual = newClient.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }
}
