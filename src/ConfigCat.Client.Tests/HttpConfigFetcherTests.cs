using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class HttpConfigFetcherTests
    {
        [TestMethod]
        public async Task HttpConfigFetcher_WithCustomHttpClientHandler_ShouldUsePassedHandler()
        {
            // Arrange

            var myHandler = new MyFakeHttpClientHandler();

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new MyCounterLogger(), myHandler, Mock.Of<IConfigDeserializer>(), false);

            // Act

            await instance.Fetch(ProjectConfig.Empty);

            // Assert

            Assert.AreEqual(1, myHandler.SendInvokeCount);
        }

        [TestMethod]
        public void HttpConfigFetcher_WithCustomHttpClientHandler_HandlersDisposeShouldNotInvoke()
        {
            // Arrange

            var myHandler = new MyFakeHttpClientHandler();

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new MyCounterLogger(), myHandler, Mock.Of<IConfigDeserializer>(), false);

            // Act

            instance.Dispose();

            // Assert

            Assert.IsFalse(myHandler.Disposed);
        }

        [TestMethod]
        public async Task HttpConfigFetcher_ResponseHttpCodeIsUnexpected_ShouldReturnsPassedConfig()
        {
            // Arrange

            var myHandler = new MyFakeHttpClientHandler(HttpStatusCode.Forbidden);

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new MyCounterLogger(), myHandler, Mock.Of<IConfigDeserializer>(), false);

            var lastConfig = new ProjectConfig("{ }", DateTime.UtcNow, "\"ETAG\"");

            // Act

            var actual = await instance.Fetch(lastConfig);

            // Assert

            Assert.AreEqual(lastConfig, actual);
        }

        [TestMethod]
        public async Task HttpConfigFetcher_ThrowAnException_ShouldReturnPassedConfig()
        {
            // Arrange

            var myHandler = new ExceptionThrowerHttpClientHandler(new WebException());

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new MyCounterLogger(), myHandler, Mock.Of<IConfigDeserializer>(), false);

            var lastConfig = new ProjectConfig("{ }", DateTime.UtcNow, "\"ETAG\"");

            // Act

            var actual = await instance.Fetch(lastConfig);

            // Assert

            Assert.AreEqual(lastConfig, actual);
        }
    }
}
