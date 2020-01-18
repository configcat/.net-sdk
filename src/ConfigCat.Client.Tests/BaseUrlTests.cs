using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class BaseUrlTests
    {
        private const string APIKEY = "configV4-test-apikey";

        private readonly Uri workingBaseUrl = new Uri("https://testcdn.configcat.com");
        private readonly Uri notWorkingBaseUrl = new Uri("https://thiswillnotwork.configcat.com");


        [TestMethod]
        public void WIP()
        {
            var client = ConfigCatClientBuilder.Initialize(apiKey: "cc")
                .WithLogger(new ConsoleLogger(LogLevel.Debug))
                .WithManualPoll()
                .Create();

            client.ForceRefresh();

            var actual = client.GetValue("isAwesomeFeatureEnabled", false);
            Assert.AreEqual(true, actual);

            client.ForceRefresh();

            var actual2 = client.GetValue("isAwesomeFeatureEnabled", false);
            Assert.AreEqual(false, actual2);
        }

        [TestMethod]
        public void BaseUrl_Override_AutoPoll_Works()
        {
            var client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithAutoPoll()
                .WithBaseUrl(this.workingBaseUrl)
                .Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithAutoPoll()
                .WithBaseUrl(this.notWorkingBaseUrl)
                .Create();

            actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }

        [TestMethod]
        public void BaseUrl_Override_ManualPoll_Works()
        {
            var client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithManualPoll()
                .WithBaseUrl(this.workingBaseUrl)
                .Create();
            client.ForceRefresh();
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithManualPoll()
                .WithBaseUrl(this.notWorkingBaseUrl)
                .Create();
            client.ForceRefresh();
            actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }

        [TestMethod]
        public void BaseUrl_Override_LazyLoad_Works()
        {
            var client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithLazyLoad()
                .WithBaseUrl(this.workingBaseUrl)
                .Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithLazyLoad()
                .WithBaseUrl(this.notWorkingBaseUrl)
                .Create();

            actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }
}
