using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests;

internal sealed class ExceptionThrowerHttpClientHandler : HttpClientHandler
{
    private readonly Exception exception;
    private readonly TimeSpan? delay;

    public byte SendInvokeCount { get; private set; } = 0;

    public ExceptionThrowerHttpClientHandler(Exception ex = null, TimeSpan? delay = null)
    {
        this.exception = ex ?? new NotImplementedException();
        this.delay = delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (this.delay is not null)
            await Task.Delay(this.delay.Value, cancellationToken);

        SendInvokeCount++;

        throw this.exception;
    }
}
