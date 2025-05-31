using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client;

internal class HttpClientConfigFetcher : IConfigCatConfigFetcher
{
    private static readonly object ObjectDisposedToken = false.AsCachedObject();

    private readonly HttpClientHandler? httpClientHandler;
    private volatile object? httpClient; // either null or a HttpClient or ObjectDisposedToken

    public HttpClientConfigFetcher(HttpClientHandler? httpClientHandler)
    {
        this.httpClientHandler = httpClientHandler;
    }

    public void Dispose()
    {
        var httpClientObj = Interlocked.Exchange(ref this.httpClient, ObjectDisposedToken);
        (httpClientObj as HttpClient)?.Dispose();
    }

    private void ResetClient(HttpClient currentHttpClient)
    {
        _ = Interlocked.CompareExchange(ref this.httpClient, value: null, comparand: currentHttpClient);
    }

    private HttpClient EnsureClient(TimeSpan timeout)
    {
        if (this.httpClient is not { } httpClientObj)
        {
            var newHttpClient = CreateClient(timeout);
            httpClientObj = Interlocked.CompareExchange(ref this.httpClient, newHttpClient, comparand: null) ?? newHttpClient;
        }

        var httpClient = httpClientObj as HttpClient
            ?? throw new ObjectDisposedException(nameof(HttpClientConfigFetcher));

        // NOTE: Request timeout should not change during the lifetime of the client instance.
        Debug.Assert(httpClient.Timeout == timeout, "Request timeout changed.");

        return httpClient;
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
        var httpClient = EnsureClient(request.Timeout);

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = request.Uri,
        };

        for (int i = 0, n = request.Headers.Count; i < n; i++)
        {
            var header = request.Headers[i];
            httpRequest.Headers.Add(header.Key, header.Value);
        }

        if (request.LastETag is not null)
        {
            httpRequest.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(request.LastETag));
        }

        try
        {
            var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
#if NET5_0_OR_GREATER
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
#else
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(TaskShim.ContinueOnCapturedContext);
#endif

                return new FetchResponse(httpResponse, httpResponseBody);
            }
            else
            {
                var response = new FetchResponse(httpResponse);
                if (!response.IsExpected)
                {
                    ResetClient(httpClient);
                }
                return response;
            }
        }
        catch (ObjectDisposedException ex)
        {
            // It is possible that HttpClient.Dispose is called between EnsureClient and SendAsync. In such cases SendAsync will throw
            // an ObjectDisposedException. Wrap it in an OperationCanceledException and let callers deal with it.
            throw new OperationCanceledException(message: null, ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested
            // NOTE: Request timeout and calling HttpClient.Dispose while SendAsync is in progress both result in a TaskCanceledException.
            // It seems there's no built-in way to tell these cases apart.
            && !ReferenceEquals(this.httpClient, ObjectDisposedToken))
        {
            ResetClient(httpClient);
            throw FetchErrorException.Timeout(httpClient.Timeout, ex);
        }
        catch (OperationCanceledException)
        {
            // If the cancellation has been requested externally or happened because of HttpClient.Dispose, let the exception bubble up.
            throw;
        }
        catch (HttpRequestException ex)
        {
            // Let the HttpClient to be recreated so it can pick up potentially changed DNS entries
            // (see also https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#dns-behavior).
            ResetClient(httpClient);
            throw FetchErrorException.Failure((ex.InnerException as WebException)?.Status, ex);
        }
        catch
        {
            ResetClient(httpClient);
            throw;
        }
    }
}
