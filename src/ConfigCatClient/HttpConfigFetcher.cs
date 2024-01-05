using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client;

internal sealed class HttpConfigFetcher : IConfigFetcher, IDisposable
{
    private readonly object syncObj = new();
    private readonly string productVersion;
    private readonly LoggerWrapper logger;

    private readonly HttpClientHandler? httpClientHandler;
    private readonly bool isCustomUri;
    private readonly TimeSpan timeout;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private HttpClient httpClient;
    internal Task<FetchResult>? pendingFetch;

    private Uri requestUri;

    public HttpConfigFetcher(Uri requestUri, string productVersion, LoggerWrapper logger,
        HttpClientHandler? httpClientHandler, bool isCustomUri, TimeSpan timeout)
    {
        this.requestUri = requestUri;
        this.productVersion = productVersion;
        this.logger = logger;
        this.httpClientHandler = httpClientHandler;
        this.isCustomUri = isCustomUri;
        this.timeout = timeout;
        this.httpClient = CreateHttpClient();
    }

    public FetchResult Fetch(ProjectConfig lastConfig)
    {
        // NOTE: We go sync over async here, however it's safe to do that in this case as
        // BeginFetchOrJoinPending will run the fetch operation on the thread pool,
        // where there's no synchronization context which awaits want to return to,
        // thus, they won't try to run continuations on this thread where we're block waiting.
        return BeginFetchOrJoinPending(lastConfig).GetAwaiter().GetResult();
    }

    public Task<FetchResult> FetchAsync(ProjectConfig lastConfig, CancellationToken cancellationToken = default)
    {
        return BeginFetchOrJoinPending(lastConfig).WaitAsync(cancellationToken);
    }

    private Task<FetchResult> BeginFetchOrJoinPending(ProjectConfig lastConfig)
    {
        lock (this.syncObj)
        {
            this.pendingFetch ??= Task.Run(async () =>
            {
                try
                {
                    return await FetchInternalAsync(lastConfig).ConfigureAwait(false);
                }
                finally
                {
                    lock (this.syncObj)
                    {
                        this.pendingFetch = null;
                    }
                }
            });

            return this.pendingFetch;
        }
    }

    private async ValueTask<FetchResult> FetchInternalAsync(ProjectConfig lastConfig)
    {
        FormattableLogMessage logMessage;
        Exception errorException;
        try
        {
            var responseWithBody = await FetchRequestAsync(lastConfig.HttpETag, this.requestUri).ConfigureAwait(false);

            var response = responseWithBody.Response;

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var config = responseWithBody.Config;
                    if (config is null)
                    {
                        var exception = responseWithBody.Exception;
                        logMessage = this.logger.FetchReceived200WithInvalidBody(exception);
                        return FetchResult.Failure(lastConfig, logMessage.InvariantFormattedMessage, exception);
                    }

                    return FetchResult.Success(new ProjectConfig
                    (
                        httpETag: response.Headers.ETag?.Tag,
                        configJson: responseWithBody.ResponseBody,
                        config: config,
                        timeStamp: ProjectConfig.GenerateTimeStamp()
                    ));

                case HttpStatusCode.NotModified:
                    if (lastConfig.IsEmpty)
                    {
                        logMessage = this.logger.FetchReceived304WhenLocalCacheIsEmpty((int)response.StatusCode, response.ReasonPhrase);
                        return FetchResult.Failure(lastConfig, logMessage.InvariantFormattedMessage);
                    }

                    return FetchResult.NotModified(lastConfig.With(ProjectConfig.GenerateTimeStamp()));

                case HttpStatusCode.Forbidden:
                case HttpStatusCode.NotFound:
                    logMessage = this.logger.FetchFailedDueToInvalidSdkKey();

                    // We update the timestamp for extra protection against flooding.
                    return FetchResult.Failure(lastConfig.With(ProjectConfig.GenerateTimeStamp()), logMessage.InvariantFormattedMessage);

                default:
                    logMessage = this.logger.FetchFailedDueToUnexpectedHttpResponse((int)response.StatusCode, response.ReasonPhrase);

                    ReInitializeHttpClient();
                    return FetchResult.Failure(lastConfig, logMessage.InvariantFormattedMessage);
            }
        }
        catch (OperationCanceledException) when (this.cancellationTokenSource.IsCancellationRequested)
        {
            // NOTE: Unfortunately, we can't check the CancellationToken property of the exception in the when condition above because
            // it seems that HttpClient.SendAsync combines our token with another one under the hood (at least, in runtimes earlier than .NET 6),
            // so we'd get a Linked2CancellationTokenSource here instead of our token which we pass to the HttpClient.SendAsync method...

            /* do nothing on dispose cancel */
            return FetchResult.NotModified(lastConfig);
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

    private async ValueTask<ResponseWithBody> FetchRequestAsync(string? httpETag, Uri requestUri, sbyte maxExecutionCount = 3)
    {
        for (; ; maxExecutionCount--)
        {
            var request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = requestUri };

            if (httpETag is not null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(httpETag));
            }

            var response = await this.httpClient.SendAsync(request, this.cancellationTokenSource.Token).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
#if NET5_0_OR_GREATER
                var responseBody = await response.Content.ReadAsStringAsync(this.cancellationTokenSource.Token).ConfigureAwait(false);
#else
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

                Config config;
                try { config = Config.Deserialize(responseBody.AsMemory()); }
                catch (Exception ex) { return new ResponseWithBody(response, responseBody, ex); }

                if (config.Preferences is not null)
                {
                    var newBaseUrl = config.Preferences.BaseUrl;

                    if (newBaseUrl is null || requestUri.Host == new Uri(newBaseUrl).Host)
                    {
                        return new ResponseWithBody(response, responseBody, config);
                    }

                    RedirectMode redirect = config.Preferences.RedirectMode;

                    if (this.isCustomUri && redirect != RedirectMode.Force)
                    {
                        return new ResponseWithBody(response, responseBody, config);
                    }

                    UpdateRequestUri(new Uri(newBaseUrl));

                    if (redirect == RedirectMode.No)
                    {
                        return new ResponseWithBody(response, responseBody, config);
                    }

                    if (redirect == RedirectMode.Should)
                    {
                        this.logger.DataGovernanceIsOutOfSync();
                    }

                    if (maxExecutionCount <= 1)
                    {
                        this.logger.FetchFailedDueToRedirectLoop();
                        return new ResponseWithBody(response, responseBody, config);
                    }

                    requestUri = ReplaceUri(request.RequestUri, new Uri(newBaseUrl));
                    continue;
                }

                return new ResponseWithBody(response, responseBody, config);
            }

            return new ResponseWithBody(response, null, (Exception?)null);
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
        HttpClient client;

        if (this.httpClientHandler is null)
        {
            client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
        }
        else
        {
            client = new HttpClient(this.httpClientHandler, false);
        }

        client.Timeout = this.timeout;
        client.DefaultRequestHeaders.Add("X-ConfigCat-UserAgent",
            new ProductInfoHeaderValue("ConfigCat-Dotnet", this.productVersion).ToString());

        return client;
    }

    public void Dispose()
    {
        this.cancellationTokenSource.Cancel();
        this.httpClient?.Dispose();
    }

    private readonly struct ResponseWithBody
    {
        private readonly object? configOrException;

        public ResponseWithBody(HttpResponseMessage response, string responseBody, Config config)
        {
            Response = response;
            ResponseBody = responseBody;
            this.configOrException = config;
        }

        public ResponseWithBody(HttpResponseMessage response, string? responseBody, Exception? exception)
        {
            Response = response;
            ResponseBody = responseBody;
            this.configOrException = exception;
        }

        public HttpResponseMessage Response { get; }
        public string? ResponseBody { get; }
        public Config? Config => this.configOrException as Config;
        public Exception? Exception => this.configOrException as Exception;
    }
}
