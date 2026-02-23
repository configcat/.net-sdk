using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Shims;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

public class HttpClientConfigFetcher : IConfigCatConfigFetcher
{
    public delegate HttpClient HttpClientProvider(FetchRequest request, HttpClient? failedHttpClient = null);

    private static readonly TimeSpan HandlerRenewalThreshold = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan RequestRetryDelay = TimeSpan.FromMilliseconds(50);

    // either null (indicating disposed state)
    // or a HandlerState (internally managed handler)
    // or a HttpClientHandler (externally created handler)
    // or a HttpClientProvider (callback for full external control over HttpClient management, e.g. integration with IHttpClientFactory)
    private volatile object? handlerState;

    protected internal HttpClientConfigFetcher()
    {
        this.handlerState = new HandlerState();
    }

#if NET6_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    protected internal HttpClientConfigFetcher(IWebProxy proxy)
    {
        this.handlerState = new HandlerState(proxy ?? throw new ArgumentNullException(nameof(proxy)));
    }

    internal HttpClientConfigFetcher(HttpClientHandler httpClientHandler)
    {
        this.handlerState = httpClientHandler ?? throw new ArgumentNullException(nameof(httpClientHandler));
    }

    public HttpClientConfigFetcher(HttpClientProvider httpClientProvider)
    {
        this.handlerState = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Release whatever reference the handlerState field holds.
        var handlerState = Interlocked.Exchange(ref this.handlerState, null);

        // If using internal handler management, the handler needs to be disposed too.
        (handlerState as HandlerState)?.Handler.Dispose();
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler, TimeSpan timeout)
    {
        return new HttpClient(handler, disposeHandler: false) { Timeout = timeout };
    }

    /// <inheritdoc />
    public Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken) =>
        FetchAsync(request, logger: null, cancellationToken);

    internal async Task<FetchResponse> FetchAsync(FetchRequest request, LoggerWrapper? logger, CancellationToken cancellationToken)
    {
        var isDebugLoggingEnabled = logger is not null && logger.IsEnabled(LogLevel.Debug);

#if NET45
        Guid requestId = default;
#else
        Unsafe.SkipInit(out Guid requestId);
#endif

        if (isDebugLoggingEnabled)
        {
            requestId = Guid.NewGuid();

            logger!.LogInterpolated(LogLevel.Debug, 0,
                $"[{requestId}] Preparing request...",
                new[] { "REQUEST_ID" });
        }

        var uri = request.Uri;
        var isCustomUri = !ConfigCatClientOptions.IsCdnUri(request.Uri);
        var isRunningInBrowser = PlatformCompatibilityOptions.IsRunningInBrowser;

        if (isRunningInBrowser)
        {
            AdjustUriForBrowser(request, ref uri);
        }

        HttpClient httpClient;

        var handlerStateObj = this.handlerState;
        var handlerState = handlerStateObj as HandlerState;

        if (handlerState is not null)
        {
            httpClient = CreateHttpClient(handlerState.Handler, request.Timeout);
        }
        else if (handlerStateObj is HttpClientHandler externalHandler)
        {
            httpClient = CreateHttpClient(externalHandler, request.Timeout);
        }
        else if (handlerStateObj is HttpClientProvider httpClientProvider)
        {
            httpClient = httpClientProvider(request);
        }
        else
        {
            throw new ObjectDisposedException(nameof(HttpClientConfigFetcher));
        }

        try
        {
            const int retryLimit = 1;
            bool canRetry;

            for (var retryCount = 0; ; retryCount++)
            {
                var httpRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = uri,
                };

                if (!isRunningInBrowser)
                {
                    if (isCustomUri)
                    {
                        SetRequestHeaders(httpRequest.Headers, request.Headers);
                    }
                    else
                    {
                        SetRequestHeadersDefault(httpRequest.Headers, request.Headers);
                    }

                    if (request.LastETag is not null)
                    {
                        httpRequest.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(request.LastETag));
                    }
                }
                else if (isCustomUri)
                {
                    SetRequestHeaders(httpRequest.Headers, request.Headers);
                }

                try
                {
                    if (isDebugLoggingEnabled)
                    {
                        var proxy = handlerState is not null ? handlerState.Proxy : (handlerStateObj as HttpClientHandler)?.Proxy;
                        if (proxy is null || proxy.IsBypassed(request.Uri))
                        {
                            logger!.LogInterpolated(LogLevel.Debug, 0,
                                $"[{requestId}] Sending request... (Uri: '{httpRequest.RequestUri}', IfNoneMatch: '{httpRequest.Headers.IfNoneMatch?.ToString()}')",
                                new[] { "REQUEST_ID", "URI", "IF_NONE_MATCH" });
                        }
                        else
                        {
                            logger!.LogInterpolated(LogLevel.Debug, 0,
                                $"[{requestId}] Sending request via proxy '{proxy.GetProxy(request.Uri)}'... (Uri: '{httpRequest.RequestUri}', IfNoneMatch: '{httpRequest.Headers.IfNoneMatch?.ToString()}')",
                                new[] { "REQUEST_ID", "PROXY_URI", "URI", "IF_NONE_MATCH" });
                        }
                    }

                    using var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(TaskShim.ContinueOnCapturedContext);

                    if (isDebugLoggingEnabled)
                    {
                        logger!.LogInterpolated(LogLevel.Debug, 0,
                            $"[{requestId}] Received headers. (StatusCode: {(int)httpResponse.StatusCode}, ReasonPhrase: '{httpResponse.ReasonPhrase}', ETag: '{httpResponse.Headers.ETag?.ToString()}')",
                            new[] { "REQUEST_ID", "STATUS_CODE", "REASON_PHRASE", "ETAG" });
                    }

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
#if NET5_0_OR_GREATER
                        var httpResponseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
#else
                        var httpResponseBody = await httpResponse.Content.ReadAsStringAsync().WaitAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
#endif

                        if (isDebugLoggingEnabled)
                        {
                            logger!.LogInterpolated(LogLevel.Debug, 0,
                                $"[{requestId}] Received body. (Length: {httpResponseBody.Length})",
                                new[] { "REQUEST_ID", "LENGTH" });
                        }

                        return new FetchResponse(httpResponse, httpResponseBody);
                    }
                    else
                    {
                        var response = new FetchResponse(httpResponse);
                        if (response.IsExpected)
                        {
                            return response;
                        }

                        if (isDebugLoggingEnabled)
                        {
                            logger!.LogInterpolated(LogLevel.Debug, 0,
                                $"[{requestId}] Received unexpected status code.",
                                new[] { "REQUEST_ID" });
                        }

                        canRetry = retryCount < retryLimit;
                        RenewClient(requestId, request, ref handlerStateObj, ref handlerState, ref httpClient, canRetry);
                        if (!canRetry)
                        {
                            return response;
                        }
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    // It is possible that the handler is disposed between obtaining the reference and the call to SendAsync.
                    // In such cases SendAsync will throw an ObjectDisposedException. Wrap it in an OperationCanceledException
                    // and let callers deal with it.
                    throw WrapObjectDisposedException(ex);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    if (isDebugLoggingEnabled)
                    {
                        logger!.LogInterpolated(LogLevel.Debug, 0, ex,
                            $"[{requestId}] Request timed out.",
                            new[] { "REQUEST_ID" });
                    }

                    canRetry = retryCount < retryLimit;
                    RenewClient(requestId, request, ref handlerStateObj, ref handlerState, ref httpClient, canRetry);
                    if (!canRetry)
                    {
                        throw FetchErrorException.Timeout(httpClient.Timeout, ex);
                    }
                }
                catch (OperationCanceledException)
                {
                    // If the cancellation has been requested externally, let the exception bubble up.
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    if (isDebugLoggingEnabled)
                    {
                        logger!.LogInterpolated(LogLevel.Debug, 0, ex,
                            $"[{requestId}] Request failed.",
                            new[] { "REQUEST_ID" });
                    }

                    canRetry = retryCount < retryLimit;
                    RenewClient(requestId, request, ref handlerStateObj, ref handlerState, ref httpClient, canRetry);
                    if (!canRetry)
                    {
                        throw FetchErrorException.Failure((ex.InnerException as WebException)?.Status, ex);
                    }

                }

                // Wait a little before trying again.
                await TaskShim.Current.Delay(RequestRetryDelay, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

                if (isDebugLoggingEnabled)
                {
                    logger!.LogInterpolated(LogLevel.Debug, 0,
                        $"[{requestId}] Trying request again...",
                        new[] { "REQUEST_ID" });
                }
            }
        }
        finally { httpClient.Dispose(); }

        void RenewClient(in Guid requestId, in FetchRequest request, ref object? handlerStateObj, ref HandlerState? handlerState,
            ref HttpClient httpClient, bool canRetry)
        {
            // Attempt to renew the client so it can pick up potentially changed DNS entries.
            // See also: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#dns-behavior

            HttpClient newHttpClient;
            if (handlerState is not null)
            {
                // If handler is managed internally, try to renew the handler, i.e. create another connection pool.

                if (handlerState.TimeElapsedSinceLastRenew < HandlerRenewalThreshold)
                {
                    // NOTE: The new handler implementation (SocketsHttpHandler) doesn't immediately close the connections in
                    // the pool on dispose but keeps them around for some time in the TIME_WAIT state, according to RFC 9293.
                    // The exact length of this period depends on OS settings but usually it's 1-4 min. A reasonable threshold
                    // for recreating handlers should be enough to avoid socket exhaustion, even in extreme cases.
                    // See also: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#pooled-connections

                    handlerStateObj = this.handlerState;
                    CheckNotDisposed(handlerStateObj, throwIfDisposed: canRetry);
                    return;
                }

                var renewedHandlerState = handlerState.Renew();
                handlerStateObj = Interlocked.CompareExchange(ref this.handlerState, value: renewedHandlerState, comparand: handlerState);
                if (ReferenceEquals(handlerStateObj, handlerState))
                {
                    // NOTE: We deliberately don't dispose the original handler as that would make potential pending requests
                    // running concurrently fail. Instead, we leave it up to the handler's finalizer to clean up unmanaged
                    // resources when requests are completed and the handler is collected by GC.

                    handlerState = renewedHandlerState;
                }
                else
                {
                    renewedHandlerState.Handler.Dispose(); // just in case, although this instance wasn't used at all

                    if (CheckNotDisposed(handlerStateObj, throwIfDisposed: canRetry))
                    {
                        handlerState = (HandlerState)handlerStateObj!;
                    }
                    else
                    {
                        return;
                    }
                }

                if (!canRetry)
                {
                    return;
                }

                newHttpClient = CreateHttpClient(handlerState.Handler, request.Timeout);
            }
            else
            {
                handlerStateObj = this.handlerState;
                if (handlerStateObj is HttpClientProvider httpClientProvider)
                {
                    if (!canRetry)
                    {
                        return;
                    }

                    // If client is provided externally, give consumer the opportunity to provide another instance for retrying.

                    newHttpClient = httpClientProvider(request, httpClient);
                    if (ReferenceEquals(newHttpClient, httpClient))
                    {
                        return;
                    }
                }
                else
                {
                    CheckNotDisposed(handlerStateObj, throwIfDisposed: canRetry);
                    return;
                }
            }

            httpClient.Dispose();
            httpClient = newHttpClient;

            if (isDebugLoggingEnabled)
            {
                logger!.LogInterpolated(LogLevel.Debug, 0,
                    $"[{requestId}] Renewed HttpClient.",
                    new[] { "REQUEST_ID" });
            }
        }

        static bool CheckNotDisposed(object? handlerStateObj, bool throwIfDisposed)
        {
            return
                handlerStateObj is not null ? true
                : !throwIfDisposed ? false
                : throw WrapObjectDisposedException(new ObjectDisposedException(nameof(HttpClientConfigFetcher)));
        }

        static OperationCanceledException WrapObjectDisposedException(ObjectDisposedException ex)
        {
            return new OperationCanceledException(message: null, ex);
        }
    }

    private static void AdjustUriForBrowser(in FetchRequest request, ref Uri uri)
    {
        var userAgentHeader = request.Headers.FirstOrDefault(static kvp =>
            DefaultConfigFetcher.UserAgentHeaderName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)
            || DefaultConfigFetcher.ConfigCatUserAgentHeaderName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

        var uriBuilder = new UriBuilder(uri);

        var separator = uriBuilder.Query.Length == 0 ? "?" : "&";

        const string sdkQueryParamName = "sdk=";
        var sdkQueryParamValue = Uri.EscapeDataString(userAgentHeader.Value ?? string.Empty);

        if (request.LastETag is not null)
        {
            // We are sending the etag as a query parameter so if the browser doesn't automatically adds the If-None-Match header,
            // we can transform this query param to the header in our CDN provider.
            // (Explicitly specifying the If-None-Match header would cause an unnecessary CORS OPTIONS request.)
            uriBuilder.Query += separator + sdkQueryParamName + sdkQueryParamValue
                + "&ccetag=" + Uri.EscapeDataString(request.LastETag);
        }
        else
        {
            uriBuilder.Query += separator + sdkQueryParamName + sdkQueryParamValue;
        }

        uri = uriBuilder.Uri;
    }

    private static void SetRequestHeadersDefault(HttpRequestHeaders httpRequestHeaders, IReadOnlyList<KeyValuePair<string, string>> headers)
    {
        for (int i = 0, n = headers.Count; i < n; i++)
        {
            var header = headers[i];
            httpRequestHeaders.Add(header.Key, header.Value);
        }
    }

    protected virtual void SetRequestHeaders(HttpRequestHeaders httpRequestHeaders, IReadOnlyList<KeyValuePair<string, string>> headers)
    {
        if (!PlatformCompatibilityOptions.IsRunningInBrowser)
        {
            SetRequestHeadersDefault(httpRequestHeaders, headers);
        }
    }

    private sealed class HandlerState
    {
        public readonly HttpClientHandler Handler;
        public readonly IWebProxy? Proxy;
        private readonly TimeSpan updateTime;

        public HandlerState(IWebProxy? proxy = null)
            : this(proxy, TimeSpan.MinValue) { }

        private HandlerState(IWebProxy? proxy, TimeSpan updateTime)
        {
            var handler = new HttpClientHandler();

            if (proxy is not null)
            {
                handler.Proxy = proxy;
            }

            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            this.Handler = handler;
            // NOTE: We can't safely access the proxy instance via this.Handler.Proxy as the seter of that property may
            // throw on some platforms (e.g. in browser).
            this.Proxy = proxy;
            this.updateTime = updateTime;
        }

        public TimeSpan TimeElapsedSinceLastRenew => this.updateTime > TimeSpan.MinValue
            ? DateTimeUtils.GetMonotonicTime() - this.updateTime
            : TimeSpan.MaxValue;

        public HandlerState Renew() => new(this.Proxy, DateTimeUtils.GetMonotonicTime());
    }
}
