using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

            var myLogger = new MyCounterLogger();

            var configDeserializer = new ConfigDeserializer( myLogger );

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "config.json", "1.0", configDeserializer, new MyCounterLogger(), myHandler);

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

            var myLogger = new MyCounterLogger();

            var configDeserializer = new ConfigDeserializer( myLogger );

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "config.json", "1.0", configDeserializer, new MyCounterLogger(), myHandler);

            // Act

            instance.Dispose();

            // Assert

            Assert.IsFalse(myHandler.Disposed);
        }
    }
}
