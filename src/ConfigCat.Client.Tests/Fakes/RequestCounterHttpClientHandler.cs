using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
    internal sealed class RequestCounterHttpClientHandler : HttpClientHandler
    {
        public byte SendAsyncInvokeCount { get; private set; } = 0;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendAsyncInvokeCount++;

            return base.SendAsync(request, cancellationToken);
        }

#if NET5_0_OR_GREATER
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendAsyncInvokeCount++;

            return base.Send(request, cancellationToken);
        }
#endif

        public void Reset()
        {
            SendAsyncInvokeCount = 0;
        }
    }
}
