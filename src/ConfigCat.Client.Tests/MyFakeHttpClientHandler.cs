using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
    internal class MyFakeHttpClientHandler : HttpClientHandler
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
