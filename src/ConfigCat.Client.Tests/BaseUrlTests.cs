using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class BaseUrlTests
    {
        private const string APIKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

        private readonly Uri workingBaseUrl = new Uri("https://cdn01.configcat.com");
        private readonly Uri notWorkingBaseUrl = new Uri("https://thiswillnotwork.configcat.com");

        [TestMethod]
        public void BaseUrl_Override_AutoPoll_Works()
        {
            var client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithAutoPoll()
                .WithBaseUrl(workingBaseUrl)
                .Create();

            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithAutoPoll()
                .WithBaseUrl(notWorkingBaseUrl)
                .Create();

            actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }

        [TestMethod]
        public void BaseUrl_Override_ManualPoll_Works()
        {
            var client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithManualPoll()
                .WithBaseUrl(workingBaseUrl)
                .Create();
            client.ForceRefresh();
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithManualPoll()
                .WithBaseUrl(notWorkingBaseUrl)
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
                .WithBaseUrl(workingBaseUrl)
                .Create();
            
            var actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("Cat", actual);

            client = ConfigCatClientBuilder.Initialize(APIKEY)
                .WithLazyLoad()
                .WithBaseUrl(notWorkingBaseUrl)
                .Create();

            actual = client.GetValue("stringDefaultCat", "N/A");
            Assert.AreEqual("N/A", actual);
        }
    }
}
