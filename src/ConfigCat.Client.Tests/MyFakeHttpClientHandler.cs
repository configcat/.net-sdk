using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
    internal class MyFakeHttpClientHandler : HttpClientHandler
    {
        private readonly HttpStatusCode httpStatusCode;

        public byte SendInvokeCount { get; private set; } = 0;

        public bool Disposed { get; private set; } = false;

        public SortedList<byte, HttpRequestMessage> Requests = new SortedList<byte, HttpRequestMessage>();

        public MyFakeHttpClientHandler(HttpStatusCode httpStatusCode = HttpStatusCode.NotModified)
        {
            this.httpStatusCode = httpStatusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendInvokeCount++;

            Requests.Add(SendInvokeCount, request);

            var response = new HttpResponseMessage(this.httpStatusCode);

            return Task.FromResult(response);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposed = true;
        }
    }
}
