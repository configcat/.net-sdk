using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests;

internal sealed class RequestCounterHttpClientHandler : HttpClientHandler
{
    private int sendInvokeCount = 0;
    public byte SendInvokeCount => (byte)this.sendInvokeCount;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref this.sendInvokeCount);

        return base.SendAsync(request, cancellationToken);
    }

#if NET5_0_OR_GREATER
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref this.sendInvokeCount);

        return base.Send(request, cancellationToken);
    }
#endif

    public void Reset()
    {
        Volatile.Write(ref this.sendInvokeCount, 0);
    }
}
