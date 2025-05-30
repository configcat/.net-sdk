using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client;

internal sealed class DefaultConfigFetcher : IConfigFetcher, IDisposable
{
    private readonly string sdkKey;
    private volatile Uri baseUri;
    private readonly IReadOnlyList<KeyValuePair<string, string>> requestHeaders;
    private readonly LoggerWrapper logger;
    private readonly IConfigCatConfigFetcher configFetcher;
    private readonly bool isCustomUri;
    private readonly TimeSpan timeout;

    public DefaultConfigFetcher(string sdkKey, Uri baseUri, string productVersion, LoggerWrapper logger,
        IConfigCatConfigFetcher configFetcher, bool isCustomUri, TimeSpan timeout)
    {
        this.sdkKey = sdkKey;
        this.baseUri = baseUri;
        this.requestHeaders = new[]
        {
            new KeyValuePair<string, string>("X-ConfigCat-UserAgent", new ProductInfoHeaderValue("ConfigCat-Dotnet", productVersion).ToString())
        };
        this.logger = logger;
        this.configFetcher = configFetcher;
        this.isCustomUri = isCustomUri;
        this.timeout = timeout;
    }

    public FetchResult Fetch(ProjectConfig lastConfig)
    {
        // NOTE: This method is unused now, we keep it because of tests for now,
        // until the synchronous code paths are deleted soon.
        return TaskShim.Current.Run(() => FetchAsync(lastConfig)).GetAwaiter().GetResult();
    }

    public async Task<FetchResult> FetchAsync(ProjectConfig lastConfig, CancellationToken cancellationToken = default)
    {
        FormattableLogMessage logMessage;
        RefreshErrorCode errorCode;
        Exception errorException;
        try
        {
            var deserializedResponse = await FetchRequestAsync(lastConfig.HttpETag, cancellationToken)
                .ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            var response = deserializedResponse.Response;

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var config = deserializedResponse.Config;
                    if (config is null)
                    {
                        var exception = deserializedResponse.Exception;
                        logMessage = this.logger.FetchReceived200WithInvalidBody(response.RayId, exception);
                        return FetchResult.Failure(lastConfig, RefreshErrorCode.InvalidHttpResponseContent, logMessage.ToLazyString(), exception);
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
                        logMessage = this.logger.FetchReceived304WhenLocalCacheIsEmpty((int)response.StatusCode, response.ReasonPhrase, response.RayId);
                        return FetchResult.Failure(lastConfig, RefreshErrorCode.InvalidHttpResponseWhenLocalCacheIsEmpty, logMessage.ToLazyString());
                    }

                    return FetchResult.NotModified(lastConfig.With(ProjectConfig.GenerateTimeStamp()));

                case HttpStatusCode.Forbidden:
                case HttpStatusCode.NotFound:
                    logMessage = this.logger.FetchFailedDueToInvalidSdkKey(this.sdkKey, response.RayId);

                    // We update the timestamp for extra protection against flooding.
                    return FetchResult.Failure(lastConfig.With(ProjectConfig.GenerateTimeStamp()), RefreshErrorCode.InvalidSdkKey, logMessage.ToLazyString());

                default:
                    logMessage = this.logger.FetchFailedDueToUnexpectedHttpResponse((int)response.StatusCode, response.ReasonPhrase, response.RayId);
                    return FetchResult.Failure(lastConfig, RefreshErrorCode.UnexpectedHttpResponse, logMessage.ToLazyString());
            }
        }
        catch (OperationCanceledException)
        {
            throw;
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

        return FetchResult.Failure(lastConfig, errorCode, logMessage.ToLazyString(), errorException);
    }

    private async ValueTask<DeserializedResponse> FetchRequestAsync(string? httpETag, CancellationToken cancellationToken, sbyte maxExecutionCount = 3)
    {
        for (; ; maxExecutionCount--)
        {
            var requestUri = ConfigCatClientOptions.GetConfigUri(this.baseUri, this.sdkKey);

            var request = new FetchRequest(requestUri, httpETag, this.requestHeaders, this.timeout);

            var response = await this.configFetcher.FetchAsync(request, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new DeserializedResponse(response, (Exception?)null);
            }

            Config config;
            try { config = Config.Deserialize(response.Body.AsMemory()); }
            catch (Exception ex) { return new DeserializedResponse(response, ex); }

            if (config.Preferences is null)
            {
                return new DeserializedResponse(response, config);
            }

            Uri newBaseUri;

            if (config.Preferences.BaseUrl is not { } newBaseUrl || this.baseUri.Equals(newBaseUri = GetBaseUri(newBaseUrl)))
            {
                return new DeserializedResponse(response, config);
            }

            RedirectMode redirect = config.Preferences.RedirectMode;

            if (this.isCustomUri && redirect != RedirectMode.Force)
            {
                return new DeserializedResponse(response, config);
            }

            this.baseUri = newBaseUri;

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
                this.logger.FetchFailedDueToRedirectLoop(response.RayId);
                return new DeserializedResponse(response, config);
            }
        }

        static Uri GetBaseUri(string url)
        {
            // NOTE: Other SDKs use string concatenation to combine the base url and config path. Here we use the
            // Uri(Uri, string) constructor for that purpose, which produces similar results only if the base ends with '/'!
            if (url.Length == 0 || url[url.Length - 1] != '/')
            {
                url += "/";
            }
            return new Uri(url);
        }
    }

    public void Dispose()
    {
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
