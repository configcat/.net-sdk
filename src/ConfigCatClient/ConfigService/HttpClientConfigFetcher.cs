using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Shims;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

public sealed class HttpClientConfigFetcher : IConfigCatConfigFetcher
{
    public delegate HttpClient HttpClientProvider(FetchRequest request, HttpClient? failedHttpClient = null);

    private static readonly object ObjectDisposedToken = false.AsCachedObject();

    // NOTE: The new handler implementation (SocketsHttpHandler) doesn't immediately close the connections in the pool
    // on dispose but keeps them around for some time in the TIME_WAIT state, according to RFC 9293. The exact length
    // of this period depends on OS settings but usually it's 1-4 min. A reasonable threshold for recreating handlers
    // should be enough to avoid socket exhaustion, even in extreme cases.
    // See also: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#pooled-connections
    private static readonly TimeSpan RenewHandlerThreshold = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    private readonly HttpClientProvider? provideHttpClient;
    private volatile object? handlerState; // either null or a HandlerState or ObjectDisposedToken

    internal LoggerWrapper? logger; // initialized by ConfigCatClient constructor

    public HttpClientConfigFetcher()
    {
        this.handlerState = new HandlerState();
    }

    internal HttpClientConfigFetcher(HttpClientHandler httpClientHandler)
    {
        this.provideHttpClient = (request, _) => CreateClient(httpClientHandler, request.Timeout);
    }

    public HttpClientConfigFetcher(IWebProxy proxy)
    {
        this.handlerState = new HandlerState(proxy);
    }

    public HttpClientConfigFetcher(HttpClientProvider httpClientProvider)
    {
        this.provideHttpClient = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));
    }

    public void Dispose()
    {
        var handlerState = Interlocked.Exchange(ref this.handlerState, ObjectDisposedToken);
        (handlerState as HandlerState)?.Handler.Dispose();
    }

    private static HandlerState GetHandlerOrThrow(object? handlerStateObj)
    {
        return handlerStateObj is HandlerState handlerState
            ? handlerState
            : throw new ObjectDisposedException(nameof(HttpClientConfigFetcher));
    }

    private static HttpClient CreateClient(HttpMessageHandler handler, TimeSpan timeout)
    {
        return new HttpClient(handler, disposeHandler: false)
        {
            Timeout = timeout,
        };
    }

    /// <inheritdoc />
    public async Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken)
    {
        var logger = this.logger;
        Guid requestId = default;

        if (logger is not null && logger.IsEnabled(LogLevel.Debug))
        {
            requestId = Guid.NewGuid();

            logger?.LogInterpolated(LogLevel.Debug, 0,
                $"[{requestId}] Preparing request...",
                new[] { "REQUEST_ID" });
        }

        HttpClient httpClient;
        HandlerState? handlerState;

        var handlerStateObj = this.handlerState;
        if (handlerStateObj is not null)
        {
            handlerState = GetHandlerOrThrow(handlerStateObj);
            httpClient = CreateClient(handlerState.Handler, request.Timeout);
        }
        else
        {
            handlerState = null;
            httpClient = this.provideHttpClient!(request);
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
                    if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                    {
                        logger?.LogInterpolated(LogLevel.Debug, 0,
                            $"[{requestId}] Sending request... (Uri: '{httpRequest.RequestUri}', IfNoneMatch: '{httpRequest.Headers.IfNoneMatch?.ToString()}')",
                            new[] { "REQUEST_ID", "URI", "IF_NONE_MATCH" });
                    }

                    var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(TaskShim.ContinueOnCapturedContext);

                    if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                    {
                        logger?.LogInterpolated(LogLevel.Debug, 0,
                            $"[{requestId}] Received headers. (StatusCode: {httpResponse.StatusCode}, ReasonPhrase: '{httpResponse.ReasonPhrase}', ETag: '{httpResponse.Headers.ETag?.ToString()}')",
                            new[] { "REQUEST_ID", "STATUS_CODE", "REASON_PHRASE", "ETAG" });
                    }

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
#if NET5_0_OR_GREATER
                        var httpResponseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
#else
                        var httpResponseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(TaskShim.ContinueOnCapturedContext);
#endif

                        if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                        {
                            logger?.LogInterpolated(LogLevel.Debug, 0,
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

                        canRetry = retryCount < retryLimit;
                        RenewClient(requestId, request, ref handlerStateObj, ref handlerState, ref httpClient, canRetry);
                        if (!canRetry)
                        {
                            return response;
                        }
                        else if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                        {
                            logger?.LogInterpolated(LogLevel.Debug, 0,
                                $"[{requestId}] Received unexpected status code. Retrying...",
                                new[] { "REQUEST_ID" });
                        }
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    // It is possible that the handler is disposed between the call to GetHandlerOrThrow and the call to SendAsync.
                    // In such cases SendAsync will throw an ObjectDisposedException. Wrap it in an OperationCanceledException
                    // and let callers deal with it.
                    throw new OperationCanceledException(message: null, ex);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    canRetry = retryCount < retryLimit;
                    RenewClient(requestId, request, ref handlerStateObj, ref handlerState, ref httpClient, canRetry);
                    if (!canRetry)
                    {
                        throw FetchErrorException.Timeout(httpClient.Timeout, ex);
                    }
                    else if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                    {
                        logger?.LogInterpolated(LogLevel.Debug, 0, ex,
                            $"[{requestId}] Request timed out. Retrying...",
                            new[] { "REQUEST_ID" });
                    }
                }
                catch (OperationCanceledException)
                {
                    // If the cancellation has been requested externally, let the exception bubble up.
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    canRetry = retryCount < retryLimit;
                    RenewClient(requestId, request, ref handlerStateObj, ref handlerState, ref httpClient, canRetry);
                    if (!canRetry)
                    {
                        throw FetchErrorException.Failure((ex.InnerException as WebException)?.Status, ex);
                    }
                    else if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                    {
                        logger?.LogInterpolated(LogLevel.Debug, 0, ex,
                            $"[{requestId}] Request failed. Retrying...",
                            new[] { "REQUEST_ID" });
                    }
                }

                // Wait a little before trying again.
                await TaskShim.Current.Delay(RetryDelay, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            }
        }
        finally { httpClient.Dispose(); }

        void RenewClient(in Guid requestId, FetchRequest request, ref object? handlerStateObj, ref HandlerState? handlerState,
            ref HttpClient httpClient, bool canRetry)
        {
            // Attempt to renew the client so it can pick up potentially changed DNS entries.
            // See also: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#dns-behavior

            HttpClient newHttpClient;
            if (handlerState is not null)
            {
                // If using built-in handler management, try to renew the handler, i.e. create another connection pool.

                if (handlerState.TimeElapsedSinceLastRenew < RenewHandlerThreshold)
                {
                    handlerStateObj = this.handlerState;
                    CheckDisposed(handlerStateObj, throwIfDisposed: canRetry);
                    return;
                }

                var renewedHandlerState = handlerState.Renew();
                handlerStateObj = Interlocked.CompareExchange(ref this.handlerState, value: renewedHandlerState, comparand: handlerState);
                if (ReferenceEquals(handlerStateObj, handlerState))
                {
                    // NOTE: We deliberately don't dispose the original handler as that would make potential
                    // pending requests running concurrently on other threads fail. Instead, we leave it up to
                    // the handler's finalizer to clean up unmanaged resources when requests are completed and
                    // the handler is collected by GC.

                    handlerState = renewedHandlerState;
                }
                else if (CheckDisposed(handlerStateObj, throwIfDisposed: canRetry))
                {
                    handlerState = (HandlerState)handlerStateObj!;
                }
                else
                {
                    return;
                }

                newHttpClient = CreateClient(handlerState.Handler, request.Timeout);
            }
            else
            {
                // If client is provided externally, give consumer the opportunity to provide another instance for retrying.

                handlerStateObj = this.handlerState;
                if (!CheckDisposed(handlerStateObj, throwIfDisposed: canRetry))
                {
                    return;
                }

                newHttpClient = this.provideHttpClient!(request, httpClient);
                if (ReferenceEquals(newHttpClient, httpClient))
                {
                    return;
                }
            }

            httpClient.Dispose();
            httpClient = newHttpClient;

            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                logger?.LogInterpolated(LogLevel.Debug, 0,
                    $"[{requestId}] Renewed HttpClient.",
                    new[] { "REQUEST_ID" });
            }
        }

        static bool CheckDisposed(object? handlerStateObj, bool throwIfDisposed)
        {
            if (!ReferenceEquals(handlerStateObj, ObjectDisposedToken))
            {
                return true;
            }
            else if (!throwIfDisposed)
            {
                return false;
            }
            else
            {
                try { GetHandlerOrThrow(handlerStateObj); }
                catch (ObjectDisposedException ex) { throw new OperationCanceledException(message: null, ex); }
                throw new InvalidOperationException(); // just for keeping the compiler happy
            }
        }
    }

    private sealed class HandlerState
    {
        public readonly HttpClientHandler Handler;
        private readonly TimeSpan updateTime;

        public HandlerState(IWebProxy? proxy = null)
            : this(proxy, TimeSpan.MinValue) { }

        private HandlerState(IWebProxy? proxy, TimeSpan updateTime)
        {
            var handler = new HttpClientHandler();

            if (proxy is not null)
            {
                handler.UseProxy = true;
                handler.Proxy = proxy;
            }

            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            this.Handler = handler;
            this.updateTime = updateTime;
        }

        public TimeSpan TimeElapsedSinceLastRenew => this.updateTime > TimeSpan.MinValue
            ? DateTimeUtils.GetMonotonicTime() - this.updateTime
            : TimeSpan.MaxValue;

        public HandlerState Renew() => new(this.Handler.Proxy, DateTimeUtils.GetMonotonicTime());
    }
}
