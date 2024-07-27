using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client;

internal class HttpClientConfigFetcher : IConfigCatConfigFetcher
{
    private readonly HttpClientHandler? httpClientHandler;
    private volatile HttpClient? httpClient;

    public HttpClientConfigFetcher(HttpClientHandler? httpClientHandler)
    {
        this.httpClientHandler = httpClientHandler;
    }

    public void Dispose()
    {
        this.httpClient?.Dispose();
    }

    private HttpClient CreateClient(TimeSpan timeout)
    {
        HttpClient httpClient;

        if (this.httpClientHandler is null)
        {
            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            httpClient = new HttpClient(handler);
        }
        else
        {
            httpClient = new HttpClient(this.httpClientHandler, disposeHandler: false);
        }

        httpClient.Timeout = timeout;

        return httpClient;
    }

    public async Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken)
    {
        var httpClient = this.httpClient;
        if (httpClient is null)
        {
            httpClient = CreateClient(request.Timeout);

            if (Interlocked.CompareExchange(ref this.httpClient, httpClient, comparand: null) is { } currentHttpClient)
            {
                httpClient = currentHttpClient;
            }
        }
        else
        {
            // NOTE: Request timeout should not change during the lifetime of the client instance.
            Debug.Assert(httpClient.Timeout == request.Timeout, "Request timeout changed.");
        }

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = request.Uri,
        };

        httpRequest.Headers.Add(request.SdkInfoHeader.Key, request.SdkInfoHeader.Value);

        if (request.LastETag is not null)
        {
            httpRequest.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(request.LastETag));
        }

        try
        {
            var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
#if NET5_0_OR_GREATER
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

                return new FetchResponse(httpResponse.StatusCode, httpResponse.ReasonPhrase, httpResponse.Headers.ETag?.Tag, httpResponseBody);
            }
            else
            {
                var response = new FetchResponse(httpResponse.StatusCode, httpResponse.ReasonPhrase, eTag: null, body: null);
                if (!response.IsExpected)
                {
                    this.httpClient = null;
                }
                return response;
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // NOTE: Unfortunately, we can't check the CancellationToken property of the exception in the when condition above because
            // it seems that HttpClient.SendAsync combines our token with another one under the hood (at least, in runtimes earlier than .NET 6),
            // so we'd get a Linked2CancellationTokenSource here instead of our token which we pass to the HttpClient.SendAsync method...

            this.httpClient = null;
            throw FetchErrorException.Timeout(httpClient.Timeout, ex);
        }
        catch (HttpRequestException ex)
        {
            // Let the HttpClient to be recreated so it can pick up potentially changed DNS entries
            // (see also https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#dns-behavior).
            this.httpClient = null;
            throw FetchErrorException.Failure((ex.InnerException as WebException)?.Status, ex);
        }
        catch
        {
            this.httpClient = null;
            throw;
        }
    }
}
