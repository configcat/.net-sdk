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

/// <summary>
/// An implementation of <see cref="IConfigCatConfigFetcher"/> which uses <see cref="HttpClient"/> to perform ConfigCat config fetch operations.
/// </summary>
public class HttpClientConfigFetcher : IConfigCatConfigFetcher
{
    // NOTE: There's an edge case that's currently not handled by the default operation mode (internal handler management, see below):
    // The connection pool is reset only when some abnormal response is received (unexpected status code, timeout or network error).
    // However, when a DNS change occurs, it's possible that the original server continues normal operation without reporting any errors.
    // In that case the SDK may not detect the DNS change. This is because HttpClientHandler keeps using non-idle connections forever by default.
    // However, since this is an highly unlikely scenario, we decided to ignore it for keeping the implementation as simple as possible.
    // See also:
    // * https://www.stevejgordon.co.uk/httpclient-connection-pooling-in-dotnet-core
    // * https://makolyte.com/csharp-configuring-how-long-an-httpclient-connection-will-stay-open/

    /// <summary>
    /// Represents a method that is called by <see cref="HttpClientConfigFetcher"/> when it makes an HTTP request, if it is configured to use
    /// externally created <see cref="HttpClient"/> instances.
    /// </summary>
    /// <param name="request">The request for which a new <see cref="HttpClient"/> instance needs to be provided.</param>
    /// <param name="isRetry">Indicates whether it is a retried request.</param>
    /// <returns>The <see cref="HttpClient"/> instance to use for making the request.</returns>
    public delegate HttpClient HttpClientFactory(FetchRequest request, bool isRetry);

    // either null (indicating disposed state)
    // or a HandlerState (internally managed handler)
    // or a HttpMessageHandler (externally created handler)
    // or a HttpClientFactory (callback for full external control over HttpClient management, e.g. integration with IHttpClientFactory)
    private volatile object? handlerState;

    private protected bool isRunningInBrowser = PlatformCompatibilityOptions.IsRunningInBrowser;
    private protected TimeSpan handlerRenewalThreshold = TimeSpan.FromSeconds(30);
    private protected TimeSpan requestRetryDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientConfigFetcher"/> class that uses the default built-in HTTP connection management.
    /// </summary>
    protected internal HttpClientConfigFetcher()
    {
        this.handlerState = new HandlerState();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientConfigFetcher"/> class that uses the default built-in HTTP connection management
    /// and routes requests through a HTTP, HTTPS, SOCKS, etc. proxy.
    /// </summary>
    /// <param name="proxy">The proxy settings.</param>
#if NET6_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    protected internal HttpClientConfigFetcher(IWebProxy proxy)
    {
        this.handlerState = new HandlerState(proxy ?? throw new ArgumentNullException(nameof(proxy)));
    }

    internal HttpClientConfigFetcher(HttpMessageHandler externalHandler)
    {
        this.handlerState = externalHandler ?? throw new ArgumentNullException(nameof(externalHandler));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientConfigFetcher"/> class that allows full external control over HTTP connection management.
    /// </summary>
    /// <param name="httpClientFactory">A callback that creates <see cref="HttpClient"/> instances for making ConfigCat config download requests over HTTP.</param>
    /// <remarks>
    /// Use this only if you have a good understanding of <see href="https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines">the pitfalls of HttpClient</see>.
    /// <para>
    /// Also, please note that <see cref="HttpClientConfigFetcher"/> calls the <paramref name="httpClientFactory"/> callback to obtain a new <see cref="HttpClient"/> instance for each HTTP request,
    /// then disposes the provided <see cref="HttpClient"/> instance after finishing the request. Therefore, you usually want to create it
    /// with the <see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.-ctor?#system-net-http-httpclient-ctor(system-net-http-httpmessagehandler-system-boolean)">disposeHandler</see>
    /// parameter set to <see langword="false"/>.
    /// </para>
    /// </remarks>
    public HttpClientConfigFetcher(HttpClientFactory httpClientFactory)
    {
        this.handlerState = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="HttpClientConfigFetcher"/> and optionally
    /// disposes of the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources;
    /// <see langword="false"/> to releases only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        // Release whatever reference the handlerState field holds.
        var handlerState = Interlocked.Exchange(ref this.handlerState, null);

        // If using internal handler management, the handler needs to be disposed too.
        (handlerState as HandlerState)?.Handler.Dispose();
    }

    // For testing purposes.
    internal HttpMessageHandler? CurrentHandler => (this.handlerState as HandlerState)?.Handler;

    // This method is virtual for testing purposes.
    internal virtual HttpClient CreateHttpClient(HttpMessageHandler handler, TimeSpan timeout)
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
                "REQUEST_ID");
        }

        var uri = request.Uri;
        var isCustomUri = !ConfigCatClientOptions.IsCdnUri(request.Uri);

        if (this.isRunningInBrowser)
        {
            AdjustUriForBrowser(ref uri, request);
        }

        HttpClient httpClient;

        var handlerStateObj = this.handlerState;
        var handlerState = handlerStateObj as HandlerState;

        if (handlerState is not null)
        {
            httpClient = CreateHttpClient(handlerState.Handler, request.Timeout);
        }
        else if (handlerStateObj is HttpMessageHandler externalHandler)
        {
            httpClient = CreateHttpClient(externalHandler, request.Timeout);
        }
        else if (handlerStateObj is HttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory(request, isRetry: false);
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
                using var httpRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = uri,
                };

                if (!this.isRunningInBrowser)
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

                string? rayId = null;
                try
                {
                    if (isDebugLoggingEnabled)
                    {
                        var proxy = handlerState is not null ? handlerState.Proxy : (handlerStateObj as HttpClientHandler)?.Proxy;
                        if (proxy is null || proxy.IsBypassed(request.Uri))
                        {
                            logger!.LogInterpolated(LogLevel.Debug, 0,
                                $"[{requestId}] Sending request... (Uri: '{httpRequest.RequestUri}', IfNoneMatch: '{httpRequest.Headers.IfNoneMatch?.ToString()}')",
                                "REQUEST_ID", "URI", "IF_NONE_MATCH");
                        }
                        else
                        {
                            logger!.LogInterpolated(LogLevel.Debug, 0,
                                $"[{requestId}] Sending request via proxy '{proxy.GetProxy(request.Uri)}'... (Uri: '{httpRequest.RequestUri}', IfNoneMatch: '{httpRequest.Headers.IfNoneMatch?.ToString()}')",
                                "REQUEST_ID", "PROXY_URI", "URI", "IF_NONE_MATCH");
                        }
                    }

                    using var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(TaskShim.ContinueOnCapturedContext);

                    if (isDebugLoggingEnabled)
                    {
                        logger!.LogInterpolated(LogLevel.Debug, 0,
                            $"[{requestId}] Received headers. (StatusCode: {(int)httpResponse.StatusCode}, ReasonPhrase: '{httpResponse.ReasonPhrase}', ETag: '{httpResponse.Headers.ETag?.ToString()}')",
                            "REQUEST_ID", "STATUS_CODE", "REASON_PHRASE", "ETAG");
                    }

                    rayId = FetchResponse.GetRayId(httpResponse.Headers);

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
                                "REQUEST_ID", "LENGTH");
                        }

                        return new FetchResponse(httpResponse, rayId, httpResponseBody);
                    }
                    else
                    {
                        var response = new FetchResponse(httpResponse, rayId);
                        if (response.IsExpected)
                        {
                            return response;
                        }

                        if (isDebugLoggingEnabled)
                        {
                            logger!.LogInterpolated(LogLevel.Debug, 0,
                                $"[{requestId}] Received unexpected status code.",
                                "REQUEST_ID");
                        }

                        canRetry = retryCount < retryLimit;
                        RenewClient(ref httpClient, ref handlerState, ref handlerStateObj, requestId, request, canRetry);
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
                            "REQUEST_ID");
                    }

                    canRetry = retryCount < retryLimit;
                    RenewClient(ref httpClient, ref handlerState, ref handlerStateObj, requestId, request, canRetry);
                    if (!canRetry)
                    {
                        throw new FetchErrorException.Timeout_(httpClient.Timeout, ex, rayId);
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
                            "REQUEST_ID");
                    }

                    canRetry = retryCount < retryLimit;
                    RenewClient(ref httpClient, ref handlerState, ref handlerStateObj, requestId, request, canRetry);
                    if (!canRetry)
                    {
                        throw new FetchErrorException.Failure_((ex.InnerException as WebException)?.Status, ex, rayId);
                    }
                }

                // Wait a little before trying again.
                await TaskShim.Current.Delay(this.requestRetryDelay, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

                if (isDebugLoggingEnabled)
                {
                    logger!.LogInterpolated(LogLevel.Debug, 0,
                        $"[{requestId}] Trying request again...",
                        "REQUEST_ID");
                }
            }
        }
        finally { httpClient.Dispose(); }

        void RenewClient(ref HttpClient httpClient, ref HandlerState? handlerState, ref object? handlerStateObj, in Guid requestId,
            in FetchRequest request, bool canRetry)
        {
            // Attempt to renew the client so it can pick up potentially changed DNS entries.
            // See also: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#dns-behavior

            if (handlerState is not null)
            {
                // If handler is managed internally, try to renew the handler, i.e. create another connection pool.

                if (handlerState.TimeElapsedSinceLastRenew < this.handlerRenewalThreshold)
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

                httpClient.Dispose();
                httpClient = CreateHttpClient(handlerState.Handler, request.Timeout);
            }
            else
            {
                handlerStateObj = this.handlerState;
                if (handlerStateObj is HttpClientFactory httpClientFactory)
                {
                    if (!canRetry)
                    {
                        return;
                    }

                    // If client is created externally, give consumer the opportunity to create another instance for retrying.

                    httpClient.Dispose();
                    httpClient = httpClientFactory(request, isRetry: true);
                }
                else
                {
                    CheckNotDisposed(handlerStateObj, throwIfDisposed: canRetry);
                    return;
                }
            }

            if (isDebugLoggingEnabled)
            {
                logger!.LogInterpolated(LogLevel.Debug, 0,
                    $"[{requestId}] Renewed HttpClient.",
                    "REQUEST_ID");
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

    internal static void AdjustUriForBrowser(ref Uri uri, in FetchRequest request)
    {
        var userAgentHeader = request.Headers.FirstOrDefault(static kvp =>
            DefaultConfigFetcher.UserAgentHeaderName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)
            || DefaultConfigFetcher.ConfigCatUserAgentHeaderName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

        var sdkQueryParamValue = Uri.EscapeDataString(userAgentHeader.Value ?? string.Empty);

        var absoluteUri = uri.IsAbsoluteUri ? uri : new Uri(new Uri("https://x"), uri);

        // NOTE: We are sending the etag as a query parameter so if the browser doesn't automatically adds
        // the If-None-Match header, we can transform this query param to the header in our CDN provider.
        // (Explicitly specifying the If-None-Match header would cause an unnecessary CORS OPTIONS request.)

        var adjustedUri = absoluteUri.GetComponents(UriComponents.HttpRequestUrl & ~UriComponents.Query, UriFormat.UriEscaped);
        var query = absoluteUri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);

        if (query.Length == 0)
        {
            adjustedUri = request.LastETag is not null
                ? $"{adjustedUri}?{DefaultConfigFetcher.SdkQueryParamName}={sdkQueryParamValue}&{DefaultConfigFetcher.ETagQueryParamName}={Uri.EscapeDataString(request.LastETag)}"
                : string.Concat(adjustedUri, "?" + DefaultConfigFetcher.SdkQueryParamName + "=", sdkQueryParamValue);
        }
        else
        {
            adjustedUri =
                $"{adjustedUri}?{DefaultConfigFetcher.SdkQueryParamName}={sdkQueryParamValue}&{DefaultConfigFetcher.ETagQueryParamName}={Uri.EscapeDataString(request.LastETag ?? string.Empty)}&{query}";
        }

        absoluteUri = new Uri(adjustedUri, UriKind.Absolute);
        uri = uri.IsAbsoluteUri ? absoluteUri : new Uri(absoluteUri.PathAndQuery, UriKind.Relative);
    }

    private static void SetRequestHeadersDefault(HttpRequestHeaders httpRequestHeaders, IReadOnlyList<KeyValuePair<string, string>> headers)
    {
        for (int i = 0, n = headers.Count; i < n; i++)
        {
            var header = headers[i];
            httpRequestHeaders.Add(header.Key, header.Value);
        }
    }

    /// <summary>
    /// Provides an opportunity to customize the HTTP headers for the request.
    /// Called only if the SDK is configured to use a custom (non-ConfigCat CDN) server.
    /// </summary>
    /// <param name="httpRequestHeaders">The headers to send with the request.</param>
    /// <param name="headers">A set of default headers. Make sure that these are sent if you use ConfigCat Proxy.</param>
    protected virtual void SetRequestHeaders(HttpRequestHeaders httpRequestHeaders, IReadOnlyList<KeyValuePair<string, string>> headers)
    {
        if (!this.isRunningInBrowser)
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
            // NOTE: We can't safely access the proxy instance via this.Handler.Proxy as the getter of that property may
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
