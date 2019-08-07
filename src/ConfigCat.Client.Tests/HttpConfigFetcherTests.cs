using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
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

            var myHandler = new MyFakeHandler();

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new NullLoggerFactory(), myHandler);

            // Act

            await instance.Fetch(ProjectConfig.Empty);

            // Assert

            Assert.AreEqual(1, myHandler.SendInvokeCount);
        }

        [TestMethod]
        public void HttpConfigFetcher_WithCustomHttpClientHandler_HandlersDisposeShouldNotInvoke()
        {
            // Arrange

            var myHandler = new MyFakeHandler();

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new NullLoggerFactory(), myHandler);

            // Act

            instance.Dispose();

            // Assert

            Assert.IsFalse(myHandler.Disposed);
        }

        internal class MyFakeHandler : HttpClientHandler
        {
            public byte SendInvokeCount { get; private set; } = 0;

            public bool Disposed { get; private set; } = false;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                SendInvokeCount++;

                var response = new HttpResponseMessage(HttpStatusCode.NotModified);

                return Task.FromResult(response);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                Disposed = true;
            }
        }
    }
}
