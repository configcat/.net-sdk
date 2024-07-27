using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client;

internal sealed class DefaultConfigFetcher : IConfigFetcher, IDisposable
{
    private readonly object syncObj = new();
    private readonly KeyValuePair<string, string> sdkInfoHeader;
    private readonly LoggerWrapper logger;
    private readonly IConfigCatConfigFetcher configFetcher;
    private readonly bool isCustomUri;
    private readonly TimeSpan timeout;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    internal Task<FetchResult>? pendingFetch;

    private Uri requestUri;

    public DefaultConfigFetcher(Uri requestUri, string productVersion, LoggerWrapper logger,
        IConfigCatConfigFetcher configFetcher, bool isCustomUri, TimeSpan timeout)
    {
        this.requestUri = requestUri;
        this.sdkInfoHeader = new KeyValuePair<string, string>(
            "X-ConfigCat-UserAgent",
            new ProductInfoHeaderValue("ConfigCat-Dotnet", productVersion).ToString());
        this.logger = logger;
        this.configFetcher = configFetcher;
        this.isCustomUri = isCustomUri;
        this.timeout = timeout;
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
            this.pendingFetch ??= TaskShim.Current.Run(async () =>
            {
                try
                {
                    return await FetchInternalAsync(lastConfig).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
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
        RefreshErrorCode errorCode;
        Exception errorException;
        try
        {
            var deserializedResponse = await FetchRequestAsync(lastConfig.HttpETag, this.requestUri).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            var response = deserializedResponse.Response;

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var config = deserializedResponse.Config;
                    if (config is null)
                    {
                        var exception = deserializedResponse.Exception;
                        logMessage = this.logger.FetchReceived200WithInvalidBody(exception);
                        return FetchResult.Failure(lastConfig, RefreshErrorCode.InvalidHttpResponseContent, logMessage.InvariantFormattedMessage, exception);
                    }

                    return FetchResult.Success(new ProjectConfig
                    (
                        httpETag: response.ETag,
                        configJson: response.Body,
                        config: config,
                        timeStamp: ProjectConfig.GenerateTimeStamp()
                    ));

                case HttpStatusCode.NotModified:
                    if (lastConfig.IsEmpty)
                    {
                        logMessage = this.logger.FetchReceived304WhenLocalCacheIsEmpty((int)response.StatusCode, response.ReasonPhrase);
                        return FetchResult.Failure(lastConfig, RefreshErrorCode.InvalidHttpResponseWhenLocalCacheIsEmpty, logMessage.InvariantFormattedMessage);
                    }

                    return FetchResult.NotModified(lastConfig.With(ProjectConfig.GenerateTimeStamp()));

                case HttpStatusCode.Forbidden:
                case HttpStatusCode.NotFound:
                    logMessage = this.logger.FetchFailedDueToInvalidSdkKey();

                    // We update the timestamp for extra protection against flooding.
                    return FetchResult.Failure(lastConfig.With(ProjectConfig.GenerateTimeStamp()), RefreshErrorCode.InvalidSdkKey, logMessage.InvariantFormattedMessage);

                default:
                    logMessage = this.logger.FetchFailedDueToUnexpectedHttpResponse((int)response.StatusCode, response.ReasonPhrase);
                    return FetchResult.Failure(lastConfig, RefreshErrorCode.UnexpectedHttpResponse, logMessage.InvariantFormattedMessage);
            }
        }
        catch (OperationCanceledException)
        {
            /* do nothing on dispose cancel */
            return FetchResult.NotModified(lastConfig);
        }
        catch (FetchErrorException.Timeout_ ex)
        {
            logMessage = this.logger.FetchFailedDueToRequestTimeout(ex.Timeout, ex);
            errorCode = RefreshErrorCode.HttpRequestTimeout;
            errorException = ex;
        }
        catch (FetchErrorException.Failure_ ex) when (ex.Status == WebExceptionStatus.SecureChannelFailure)
        {
            logMessage = this.logger.EstablishingSecureConnectionFailed(ex);
            errorCode = RefreshErrorCode.HttpRequestFailure;
            errorException = ex;
        }
        catch (Exception ex)
        {
            logMessage = this.logger.FetchFailedDueToUnexpectedError(ex);
            errorCode = RefreshErrorCode.HttpRequestFailure;
            errorException = ex;
        }

        return FetchResult.Failure(lastConfig, errorCode, logMessage.InvariantFormattedMessage, errorException);
    }

    private async ValueTask<DeserializedResponse> FetchRequestAsync(string? httpETag, Uri requestUri, sbyte maxExecutionCount = 3)
    {
        for (; ; maxExecutionCount--)
        {
            var request = new FetchRequest(this.requestUri, httpETag, this.sdkInfoHeader, this.timeout);

            var response = await this.configFetcher.FetchAsync(request, this.cancellationTokenSource.Token).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Config config;
                try { config = Config.Deserialize(response.Body.AsMemory()); }
                catch (Exception ex) { return new DeserializedResponse(response, ex); }

                if (config.Preferences is not null)
                {
                    var newBaseUrl = config.Preferences.BaseUrl;

                    if (newBaseUrl is null || requestUri.Host == new Uri(newBaseUrl).Host)
                    {
                        return new DeserializedResponse(response, config);
                    }

                    RedirectMode redirect = config.Preferences.RedirectMode;

                    if (this.isCustomUri && redirect != RedirectMode.Force)
                    {
                        return new DeserializedResponse(response, config);
                    }

                    UpdateRequestUri(new Uri(newBaseUrl));

                    if (redirect == RedirectMode.No)
                    {
                        return new DeserializedResponse(response, config);
                    }

                    if (redirect == RedirectMode.Should)
                    {
                        this.logger.DataGovernanceIsOutOfSync();
                    }

                    if (maxExecutionCount <= 1)
                    {
                        this.logger.FetchFailedDueToRedirectLoop();
                        return new DeserializedResponse(response, config);
                    }

                    requestUri = ReplaceUri(request.Uri, new Uri(newBaseUrl));
                    continue;
                }

                return new DeserializedResponse(response, config);
            }

            return new DeserializedResponse(response, (Exception?)null);
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

    public void Dispose()
    {
        this.cancellationTokenSource.Cancel();
        this.configFetcher.Dispose();
    }

    private readonly struct DeserializedResponse
    {
        private readonly object? configOrException;

        public DeserializedResponse(in FetchResponse response, Config config)
        {
            Response = response;
            this.configOrException = config;
        }

        public DeserializedResponse(in FetchResponse response, Exception? exception)
        {
            Response = response;
            this.configOrException = exception;
        }

        public FetchResponse Response { get; }
        public Config? Config => this.configOrException as Config;
        public Exception? Exception => this.configOrException as Exception;
    }
}
