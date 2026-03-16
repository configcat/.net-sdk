using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests;

internal sealed class ExceptionThrowerHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception exception;
    private readonly TimeSpan? delay;

    private int sendInvokeCount = 0;
    public byte SendInvokeCount => (byte)this.sendInvokeCount;

    public ExceptionThrowerHttpMessageHandler(Exception? ex = null, TimeSpan? delay = null)
    {
        this.exception = ex ?? new NotImplementedException();
        this.delay = delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref this.sendInvokeCount);

        if (this.delay is not null)
            await Task.Delay(this.delay.Value, cancellationToken);

        throw this.exception;
    }

#if NET5_0_OR_GREATER
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref this.sendInvokeCount);

        if (this.delay is not null)
            Task.Delay(this.delay.Value, cancellationToken).Wait();

        throw this.exception;
    }
#endif
}
