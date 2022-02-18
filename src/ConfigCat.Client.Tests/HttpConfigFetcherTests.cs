using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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

            var myHandler = new FakeHttpClientHandler();

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger(), myHandler, Mock.Of<IConfigDeserializer>(), false,
                TimeSpan.FromSeconds(30));

            // Act

            await instance.FetchAsync(ProjectConfig.Empty);

            // Assert

            Assert.AreEqual(1, myHandler.SendInvokeCount);
        }

        [TestMethod]
        public void HttpConfigFetcher_WithCustomHttpClientHandler_HandlersDisposeShouldNotInvoke()
        {
            // Arrange

            var myHandler = new FakeHttpClientHandler();

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger(), myHandler, Mock.Of<IConfigDeserializer>(), false,
                TimeSpan.FromSeconds(30));

            // Act

            instance.Dispose();

            // Assert

            Assert.IsFalse(myHandler.Disposed);
        }

        [TestMethod]
        public async Task HttpConfigFetcher_ResponseHttpCodeIsUnexpected_ShouldReturnsPassedConfig()
        {
            // Arrange

            var myHandler = new FakeHttpClientHandler(HttpStatusCode.Forbidden);

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger(), myHandler, Mock.Of<IConfigDeserializer>(), false,
                TimeSpan.FromSeconds(30));

            var lastConfig = new ProjectConfig("{ }", DateTime.UtcNow, "\"ETAG\"");

            // Act

            var actual = await instance.FetchAsync(lastConfig);

            // Assert

            Assert.AreEqual(lastConfig, actual);
        }

        [TestMethod]
        public async Task HttpConfigFetcher_ThrowAnException_ShouldReturnPassedConfig()
        {
            // Arrange

            var myHandler = new ExceptionThrowerHttpClientHandler(new WebException());

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger(), myHandler, Mock.Of<IConfigDeserializer>(), false,
                TimeSpan.FromSeconds(30));

            var lastConfig = new ProjectConfig("{ }", DateTime.UtcNow, "\"ETAG\"");

            // Act

            var actual = await instance.FetchAsync(lastConfig);

            // Assert

            Assert.AreEqual(lastConfig, actual);
        }
    }
}
