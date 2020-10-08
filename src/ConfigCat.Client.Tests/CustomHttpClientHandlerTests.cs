using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestCategory(TestCategories.Integration)]
    [TestClass]
    public class CustomHttpClientHandlerTests
    {
        private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";
        private readonly MyHttpClientHandler httpClientHandler = new MyHttpClientHandler();

        [TestInitialize]
        public void TestInit()
        {
            httpClientHandler.Reset();
        }

        [TestMethod]
        public void AutoPoll_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation()
        {
            // Arrange

            var client = ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithDataGovernance(DataGovernance.EuOnly)
                .WithAutoPoll()
                .WithHttpClientHandler(httpClientHandler)
                .Create();

            // Act

            var actual = client.GetValue("stringDefaultCat", "N/A");

            // Assert

            Assert.AreEqual("Cat", actual);
            Assert.IsTrue(httpClientHandler.SendAsyncInvokeCount > 0);
        }

        [TestMethod]
        public void ManualPoll_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation()
        {
            // Arrange

            var client = ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithDataGovernance(DataGovernance.EuOnly)
                .WithManualPoll()
                .WithHttpClientHandler(httpClientHandler)
                .Create();

            // Act

            client.ForceRefresh();
            var actual = client.GetValue("stringDefaultCat", "N/A");

            // Assert

            Assert.AreEqual("Cat", actual);
            Assert.AreEqual(1, httpClientHandler.SendAsyncInvokeCount);
        }

        [TestMethod]
        public void LazyLoad_WithHttpClientHandlerOverride_ShouldReturnCatUseCustomImplementation()
        {
            // Arrange

            var client = ConfigCatClientBuilder.Initialize(SDKKEY)
                .WithDataGovernance(DataGovernance.EuOnly)
                .WithLazyLoad()
                .WithHttpClientHandler(httpClientHandler)
                .Create();

            // Act

            var actual = client.GetValue("stringDefaultCat", "N/A");

            // Assert

            Assert.AreEqual("Cat", actual);
            Assert.AreEqual(1, httpClientHandler.SendAsyncInvokeCount);
        }
    }
}
