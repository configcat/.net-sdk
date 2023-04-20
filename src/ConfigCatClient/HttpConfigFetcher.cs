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
using ResponseWithBody = System.Tuple<System.Net.Http.HttpResponseMessage, ConfigCat.Client.Evaluation.SettingsWithPreferences?>;
#else
using ResponseWithBody = System.ValueTuple<System.Net.Http.HttpResponseMessage, ConfigCat.Client.Evaluation.SettingsWithPreferences?>;
#endif

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
#if NET5_0_OR_GREATER
        var valueTask = FetchInternalAsync(lastConfig, isAsync: false);
        Debug.Assert(valueTask.IsCompleted);

        // The value task holds the sync result, it's safe to call GetAwaiter().GetResult()
        return valueTask.GetAwaiter().GetResult();
#else
        // we can't synchronize HttpClient, so we have to go sync over async.
        return Syncer.Sync(() => FetchInternalAsync(lastConfig, isAsync: true).AsTask());
#endif
    }

    public Task<FetchResult> FetchAsync(ProjectConfig lastConfig, CancellationToken cancellationToken = default)
    {
        lock (this.syncObj)
        {
            this.pendingFetch ??= Task.Run(async () =>
            {
                try
                {
                    return await FetchInternalAsync(lastConfig, isAsync: true).ConfigureAwait(false);
                }
                finally
                {
                    lock (this.syncObj)
                    {
                        this.pendingFetch = null;
                    }
                }
            });

            return this.pendingFetch.WaitAsync(cancellationToken);
        }
    }

    private async ValueTask<FetchResult> FetchInternalAsync(ProjectConfig lastConfig, bool isAsync)
    {
        FormattableLogMessage logMessage;
        Exception errorException;
        try
        {
            ResponseWithBody responseWithBody;
#if NET5_0_OR_GREATER
            if (isAsync)
            {
                responseWithBody = await FetchRequestAsync(lastConfig.HttpETag, this.requestUri, isAsync: true).ConfigureAwait(false);
            }
            else
            {
                var valueTask = FetchRequestAsync(lastConfig.HttpETag, this.requestUri, isAsync: false);
                Debug.Assert(valueTask.IsCompleted);

                // The value task holds the sync result, it's safe to call GetAwaiter().GetResult()
                responseWithBody = valueTask.GetAwaiter().GetResult();
            }
#else
            responseWithBody = await FetchRequestAsync(lastConfig.HttpETag, this.requestUri, isAsync: true).ConfigureAwait(false);
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
                    (
                        httpETag: response.Headers.ETag?.Tag,
                        config: responseWithBody.Item2,
                        timeStamp: DateTime.UtcNow
                    ));

                case HttpStatusCode.NotModified:
                    if (lastConfig.IsEmpty)
                    {
                        logMessage = this.logger.FetchReceived304WhenLocalCacheIsEmpty((int)response.StatusCode, response.ReasonPhrase);
                        return FetchResult.Failure(lastConfig, logMessage.InvariantFormattedMessage);
                    }

                    return FetchResult.NotModified(lastConfig.With(timeStamp: DateTime.UtcNow));

                case HttpStatusCode.Forbidden:
                case HttpStatusCode.NotFound:
                    logMessage = this.logger.FetchFailedDueToInvalidSdkKey();

                    // We update the timestamp for extra protection against flooding.
                    return FetchResult.Failure(lastConfig.With(timeStamp: DateTime.UtcNow), logMessage.InvariantFormattedMessage);

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

    private async ValueTask<ResponseWithBody> FetchRequestAsync(string? httpETag,
        Uri requestUri, bool isAsync, sbyte maxExecutionCount = 3)
    {
        for (; ; maxExecutionCount--)
        {
            var request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = requestUri };

            if (httpETag is not null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(httpETag));
            }

            HttpResponseMessage response;
#if NET5_0_OR_GREATER
            response = isAsync
                ? await this.httpClient.SendAsync(request, this.cancellationTokenSource.Token).ConfigureAwait(false)
                : this.httpClient.Send(request, this.cancellationTokenSource.Token);
#else
            response = await this.httpClient.SendAsync(request, this.cancellationTokenSource.Token).ConfigureAwait(false);
#endif

            if (response.StatusCode == HttpStatusCode.OK)
            {
#if NET5_0_OR_GREATER
                var responseBody = isAsync
                    ? await response.Content.ReadAsStringAsync(this.cancellationTokenSource.Token).ConfigureAwait(false)
                    : response.Content.ReadAsString(this.cancellationTokenSource.Token);
#else
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

                var body = responseBody.DeserializeOrDefault<SettingsWithPreferences>();
                if (body is null)
                {
                    return new ResponseWithBody(response, null);
                }

                if (body.Preferences is not null)
                {
                    var newBaseUrl = body.Preferences.Url;

                    if (newBaseUrl is null || requestUri.Host == new Uri(newBaseUrl).Host)
                    {
                        return new ResponseWithBody(response, body);
                    }

                    RedirectMode redirect = body.Preferences.RedirectMode;

                    if (this.isCustomUri && redirect != RedirectMode.Force)
                    {
                        return new ResponseWithBody(response, body);
                    }

                    UpdateRequestUri(new Uri(newBaseUrl));

                    if (redirect == RedirectMode.No)
                    {
                        return new ResponseWithBody(response, body);
                    }

                    if (redirect == RedirectMode.Should)
                    {
                        this.logger.DataGovernanceIsOutOfSync();
                    }

                    if (maxExecutionCount <= 1)
                    {
                        this.logger.FetchFailedDueToRedirectLoop();
                        return new ResponseWithBody(response, body);
                    }

                    requestUri = ReplaceUri(request.RequestUri, new Uri(newBaseUrl));
                    continue;
                }

                return new ResponseWithBody(response, body);
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
}
