using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

#if NET45
using ResponseWithBody = System.Tuple<System.Net.Http.HttpResponseMessage, string?>;
#else
using ResponseWithBody = System.ValueTuple<System.Net.Http.HttpResponseMessage, string?>;
#endif

namespace ConfigCat.Client;

internal sealed class HttpConfigFetcher : IConfigFetcher, IDisposable
{
    private readonly object syncObj = new();
    private readonly string productVersion;
    private readonly LoggerWrapper logger;

    private readonly HttpClientHandler? httpClientHandler;
    private readonly IConfigDeserializer deserializer;
    private readonly bool isCustomUri;
    private readonly TimeSpan timeout;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private HttpClient httpClient;
    internal AsyncFetch? pendingFetch;

    private Uri requestUri;

    public HttpConfigFetcher(Uri requestUri, string productVersion, LoggerWrapper logger,
        HttpClientHandler? httpClientHandler, IConfigDeserializer deserializer, bool isCustomUri, TimeSpan timeout)
    {
        this.requestUri = requestUri;
        this.productVersion = productVersion;
        this.logger = logger;
        this.httpClientHandler = httpClientHandler;
        this.deserializer = deserializer;
        this.isCustomUri = isCustomUri;
        this.timeout = timeout;
        this.httpClient = CreateHttpClient();
    }

    public FetchResult Fetch(ProjectConfig lastConfig)
    {
#if NET5_0_OR_GREATER
        var valueTask = FetchInternalAsync(lastConfig, isAsync: false, CancellationToken.None);
        Debug.Assert(valueTask.IsCompleted);

        // The value task holds the sync result, it's safe to call GetAwaiter().GetResult()
        return valueTask.GetAwaiter().GetResult();
#else
        // we can't synchronize HttpClient, so we have to go sync over async.
        return Syncer.Sync(() => FetchInternalAsync(lastConfig, isAsync: true, CancellationToken.None).AsTask());
#endif
    }

    public Task<FetchResult> FetchAsync(ProjectConfig lastConfig, CancellationToken cancellationToken = default)
    {
        AsyncFetch? pendingFetch;

        lock (this.syncObj)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return cancellationToken.ToTask<FetchResult>();
            }

            pendingFetch = this.pendingFetch;
            if (pendingFetch is null or { ObserverCount: 0 })
            {
                this.pendingFetch = pendingFetch = new AsyncFetch(this, lastConfig, fetchFunction: static async (@this, fetch) =>
                {
                    try
                    {
                        // The fetch operation should be canceled when the config fetcher gets disposed.
                        using (@this.cancellationTokenSource.Token.Register(fetch.Cancel, useSynchronizationContext: false))
                        {
                            return await @this.FetchInternalAsync(fetch.LastConfig, isAsync: true, fetch.CancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        lock (@this.syncObj)
                        {
                            if (@this.pendingFetch == fetch)
                            {
                                @this.pendingFetch = null;
                            }
                        }
                    }
                });
            }

            pendingFetch.ObserverCount++;
        }

        return Awaited(this, pendingFetch, cancellationToken);

        static async Task<FetchResult> Awaited(HttpConfigFetcher @this, AsyncFetch pendingFetch, CancellationToken cancellationToken)
        {
            var externallyCanceled = false;

            try
            {
                return await pendingFetch.FetchTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                externallyCanceled = true;
                throw;
            }
            finally
            {
                int observerCount;

                lock (@this.syncObj)
                {
                    observerCount = --pendingFetch.ObserverCount;
                }

                if (observerCount == 0)
                {
                    // The fetch operation should also be canceled when all observers has signalled cancellation.
                    if (externallyCanceled)
                    {
                        pendingFetch.Cancel();
                    }
                    pendingFetch.Dispose();
                }
            }
        }
    }

    private async ValueTask<FetchResult> FetchInternalAsync(ProjectConfig lastConfig, bool isAsync, CancellationToken cancellationToken)
    {
        FormattableLogMessage logMessage;
        Exception errorException;
        try
        {
            ResponseWithBody responseWithBody;
#if NET5_0_OR_GREATER
            if (isAsync)
            {
                responseWithBody = await FetchRequestAsync(lastConfig, this.requestUri, isAsync: true, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var valueTask = FetchRequestAsync(lastConfig, this.requestUri, isAsync: false, cancellationToken);
                Debug.Assert(valueTask.IsCompleted);

                // The value task holds the sync result, it's safe to call GetAwaiter().GetResult()
                responseWithBody = valueTask.GetAwaiter().GetResult();
            }
#else
            responseWithBody = await FetchRequestAsync(lastConfig, this.requestUri, isAsync: true, cancellationToken).ConfigureAwait(false);
#endif

            var response = responseWithBody.Item1;

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    if (responseWithBody.Item2 is null)
                    {
                        logMessage = this.logger.FetchReceived200WithInvalidBody();
                        return FetchResult.Failure(lastConfig, logMessage.InvariantFormattedMessage);
                    }

                    return FetchResult.Success(new ProjectConfig
                    {
                        HttpETag = response.Headers.ETag?.Tag,
                        JsonString = responseWithBody.Item2,
                        TimeStamp = DateTime.UtcNow
                    });

                case HttpStatusCode.NotModified:
                    if (lastConfig.IsEmpty)
                    {
                        logMessage = this.logger.FetchReceived304WhenLocalCacheIsEmpty((int)response.StatusCode, response.ReasonPhrase);
                        return FetchResult.Failure(lastConfig, logMessage.InvariantFormattedMessage);
                    }

                    return FetchResult.NotModified(lastConfig with { TimeStamp = DateTime.UtcNow });

                case HttpStatusCode.Forbidden:
                case HttpStatusCode.NotFound:
                    logMessage = this.logger.FetchFailedDueToInvalidSdkKey();

                    // We update the timestamp for extra protection against flooding.
                    return FetchResult.Failure(lastConfig with { TimeStamp = DateTime.UtcNow }, logMessage.InvariantFormattedMessage);

                default:
                    logMessage = this.logger.FetchFailedDueToUnexpectedHttpResponse((int)response.StatusCode, response.ReasonPhrase);

                    ReInitializeHttpClient();
                    return FetchResult.Failure(lastConfig, logMessage.InvariantFormattedMessage);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // NOTE: Unfortunately, we can't check the CancellationToken property of the exception in the when condition
            // because it seems that HttpClient.SendAsync combines our token with another one under the hood (in runtimes earlier than .NET 6),
            // so we'd get a Linked2CancellationTokenSource here instead of our token which we pass to that method...

            if (this.cancellationTokenSource.IsCancellationRequested)
            {
                /* do nothing on dispose cancel */
                return FetchResult.NotModified(lastConfig);
            }

            throw;
        }
        catch (OperationCanceledException ex)
        {
            logMessage = this.logger.FetchFailedDueToRequestTimeout(this.timeout, ex);
            errorException = ex;
        }
        catch (HttpRequestException ex) when (ex.InnerException is WebException { Status: WebExceptionStatus.SecureChannelFailure })
        {
            logMessage = this.logger.EstablishingSecureConnectionFailed(ex);
            errorException = ex;
        }
        catch (Exception ex)
        {
            logMessage = this.logger.FetchFailedDueToUnexpectedError(ex);
            errorException = ex;
        }

        ReInitializeHttpClient();
        return FetchResult.Failure(lastConfig, logMessage.InvariantFormattedMessage, errorException);
    }

    private async ValueTask<ResponseWithBody> FetchRequestAsync(ProjectConfig lastConfig,
        Uri requestUri, bool isAsync, CancellationToken cancellationToken, sbyte maxExecutionCount = 3)
    {
        for (; ; maxExecutionCount--)
        {
            var request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = requestUri };

            if (lastConfig.HttpETag is not null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(lastConfig.HttpETag));
            }

            HttpResponseMessage response;
#if NET5_0_OR_GREATER
            response = isAsync
                ? await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false)
                : this.httpClient.Send(request, cancellationToken);
#else
            response = await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
#endif

            if (response.StatusCode == HttpStatusCode.OK)
            {
#if NET5_0_OR_GREATER
                var responseBody = isAsync
                    ? await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
                    : response.Content.ReadAsString(cancellationToken);
#else
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

                var httpETag = response.Headers.ETag?.Tag;

                if (!this.deserializer.TryDeserialize(responseBody, httpETag, out var body))
                    return new ResponseWithBody(response, null);

                if (body.Preferences is not null)
                {
                    var newBaseUrl = body.Preferences.Url;

                    if (newBaseUrl is null || requestUri.Host == new Uri(newBaseUrl).Host)
                    {
                        return new ResponseWithBody(response, responseBody);
                    }

                    RedirectMode redirect = body.Preferences.RedirectMode;

                    if (this.isCustomUri && redirect != RedirectMode.Force)
                    {
                        return new ResponseWithBody(response, responseBody);
                    }

                    UpdateRequestUri(new Uri(newBaseUrl));

                    if (redirect == RedirectMode.No)
                    {
                        return new ResponseWithBody(response, responseBody);
                    }

                    if (redirect == RedirectMode.Should)
                    {
                        this.logger.DataGovernanceIsOutOfSync();
                    }

                    if (maxExecutionCount <= 1)
                    {
                        this.logger.FetchFailedDueToRedirectLoop();
                        return new ResponseWithBody(response, responseBody);
                    }

                    requestUri = ReplaceUri(request.RequestUri, new Uri(newBaseUrl));
                    continue;
                }

                return new ResponseWithBody(response, responseBody);
            }

            return new ResponseWithBody(response, null);
        }
    }

    private void UpdateRequestUri(Uri newUri)
    {
        lock (this.syncObj)
        {
            this.requestUri = ReplaceUri(this.requestUri, newUri);
        }
    }

    private static Uri ReplaceUri(Uri oldUri, Uri newUri)
    {
        return new Uri(newUri, oldUri.AbsolutePath);
    }

    private void ReInitializeHttpClient()
    {
        lock (this.syncObj)
        {
            this.httpClient = CreateHttpClient();
        }
    }

    private HttpClient CreateHttpClient()
    {
        HttpClient httpClient;

        if (this.httpClientHandler is null)
        {
            httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
        }
        else
        {
            httpClient = new HttpClient(this.httpClientHandler, false);
        }

        httpClient.Timeout = this.timeout;
        httpClient.DefaultRequestHeaders.Add("X-ConfigCat-UserAgent",
            new ProductInfoHeaderValue("ConfigCat-Dotnet", this.productVersion).ToString());

        return httpClient;
    }

    public void Dispose()
    {
        this.cancellationTokenSource.Cancel();
        this.httpClient?.Dispose();
    }

    internal sealed class AsyncFetch : IDisposable
    {
        private readonly HttpConfigFetcher configFetcher;
        public readonly ProjectConfig LastConfig;
        private readonly Func<HttpConfigFetcher, AsyncFetch, Task<FetchResult>> fetchFunction;
        public readonly Task<FetchResult> FetchTask;
        private CancellationTokenSource? cancellationTokenSource;
        public readonly CancellationToken CancellationToken;
        public int ObserverCount;

        public AsyncFetch(HttpConfigFetcher configFetcher, ProjectConfig lastConfig, Func<HttpConfigFetcher, AsyncFetch, Task<FetchResult>> fetchFunction)
        {
            this.configFetcher = configFetcher;
            this.LastConfig = lastConfig;
            this.fetchFunction = fetchFunction;

            this.cancellationTokenSource = new CancellationTokenSource();
            this.CancellationToken = this.cancellationTokenSource.Token;
            this.FetchTask = Task.Run(() => this.fetchFunction(this.configFetcher, this));
        }

        public void Cancel()
        {
            if (Interlocked.Exchange(ref this.cancellationTokenSource, null) is { } cts)
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref this.cancellationTokenSource, null)?.Dispose();
        }
    }
}
