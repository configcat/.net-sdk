using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
    internal sealed class MyHttpClientHandler : HttpClientHandler
    {
        public byte SendAsyncInvokeCount { get; private set; } = 0;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendAsyncInvokeCount++;

            return base.SendAsync(request, cancellationToken);
        }

        public void Reset()
        {
            SendAsyncInvokeCount = 0;
        }
    }
}
