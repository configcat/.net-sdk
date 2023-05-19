using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests;

internal class FakeHttpClientHandler : HttpClientHandler
{
    private readonly HttpStatusCode httpStatusCode;
    private readonly string? responseContent;
    private readonly TimeSpan? delay;
    private readonly EntityTagHeaderValue? httpETag;

    public byte SendInvokeCount { get; private set; } = 0;

    public bool Disposed { get; private set; } = false;

    public SortedList<byte, HttpRequestMessage> Requests = new();

    public FakeHttpClientHandler(HttpStatusCode httpStatusCode = HttpStatusCode.NotModified, string? responseContent = null, TimeSpan? delay = null,
        EntityTagHeaderValue? httpETag = null)
    {
        this.httpStatusCode = httpStatusCode;
        this.responseContent = responseContent;
        this.httpETag = httpETag;
        this.delay = delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (this.delay is not null)
            await Task.Delay(this.delay.Value, cancellationToken);

        SendInvokeCount++;

        this.Requests.Add(SendInvokeCount, request);

        var response = new HttpResponseMessage
        {
            StatusCode = this.httpStatusCode,
            Content = this.responseContent is not null ? new StringContent(this.responseContent) : null,
        };

        if (this.httpETag is not null)
        {
            response.Headers.ETag = this.httpETag;
        }

        return response;
    }

#if NET5_0_OR_GREATER
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (this.delay is not null)
            Task.Delay(this.delay.Value, cancellationToken).Wait(cancellationToken);

        SendInvokeCount++;

        this.Requests.Add(SendInvokeCount, request);

        var response = new HttpResponseMessage
        {
            StatusCode = this.httpStatusCode,
            Content = this.responseContent is not null ? new StringContent(this.responseContent) : null,
        };

        if (this.httpETag is not null)
        {
            response.Headers.ETag = this.httpETag;
        }

        return response;
    }
#endif

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Disposed = true;
    }
}
