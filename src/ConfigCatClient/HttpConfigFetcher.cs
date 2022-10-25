using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluate;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client
{
    internal sealed class HttpConfigFetcher : IConfigFetcher, IDisposable
    {
        private readonly object lck = new();
        private readonly string productVersion;
        private readonly LoggerWrapper log;

        private readonly HttpClientHandler httpClientHandler;
        private readonly IConfigDeserializer deserializer;
        private readonly bool isCustomUri;
        private readonly TimeSpan timeout;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private HttpClient httpClient;

        private Uri requestUri;

        public HttpConfigFetcher(Uri requestUri, string productVersion, LoggerWrapper logger,
            HttpClientHandler httpClientHandler, IConfigDeserializer deserializer, bool isCustomUri, TimeSpan timeout)
        {
            this.requestUri = requestUri;
            this.productVersion = productVersion;
            this.log = logger;
            this.httpClientHandler = httpClientHandler;
            this.deserializer = deserializer;
            this.isCustomUri = isCustomUri;
            this.timeout = timeout;
            ReInitializeHttpClient();
        }

        public ProjectConfig Fetch(ProjectConfig lastConfig)
        {
#if NET5_0_OR_GREATER
            var valueTask = FetchInternalAsync(lastConfig, false);
#if DEBUG
            DebugUtils.Verify(valueTask.IsCompleted);
#endif
            // The value task holds the sync result, it's safe to call GetAwaiter().GetResult()
            return valueTask.GetAwaiter().GetResult();
#else
            // we can't synchronize HttpClient, so we have to go sync over async.
            return Syncer.Sync(() => FetchInternalAsync(lastConfig, true).AsTask());
#endif
        }

        public Task<ProjectConfig> FetchAsync(ProjectConfig lastConfig)
        {
            return FetchInternalAsync(lastConfig, true).AsTask();
        }

        private async ValueTask<ProjectConfig> FetchInternalAsync(ProjectConfig lastConfig, bool isAsync)
        {
            try
            {
#if NET5_0_OR_GREATER
                Tuple<HttpResponseMessage, string> fetchResult;
                if (isAsync)
                {
                    fetchResult = await FetchRequest(lastConfig, this.requestUri, true).ConfigureAwait(false);
                }
                else
                {
                    var valueTask = FetchRequest(lastConfig, this.requestUri, false);
#if DEBUG
                    DebugUtils.Verify(valueTask.IsCompleted);
#endif
                    // The value task holds the sync result, it's safe to call GetAwaiter().GetResult()
                    fetchResult = valueTask.GetAwaiter().GetResult();
                }
#else
                var fetchResult = await FetchRequest(lastConfig, this.requestUri, true)
                    .ConfigureAwait(false);
#endif

                var response = fetchResult.Item1;

                if (response is { IsSuccessStatusCode: true })
                {
                    return new ProjectConfig
                    {
                        HttpETag = response.Headers.ETag?.Tag,
                        JsonString = fetchResult.Item2,
                        TimeStamp = DateTime.UtcNow
                    };
                }
                else
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.NotModified:
                            break;
                        case HttpStatusCode.NotFound:
                            this.log.Error("Double-check your SDK Key at https://app.configcat.com/sdkkey");
                            break;
                        default:
                            this.ReInitializeHttpClient();
                            break;
                    }

                    // We update the timestamp even if a status code other than 304 NotModified is returned
                    // for extra protection against flooding.
                    return lastConfig with
                    {
                        TimeStamp = DateTime.UtcNow
                    };
                }
            }
            catch (OperationCanceledException) when (this.cancellationTokenSource.IsCancellationRequested)
            {
                /* do nothing on dispose cancel */
            }
            catch (OperationCanceledException) when (!this.cancellationTokenSource.IsCancellationRequested)
            {
                this.log.Error($"Http timeout {this.timeout} reached.");
                this.ReInitializeHttpClient();
            }
            catch (HttpRequestException ex) when (ex.InnerException is WebException { Status: WebExceptionStatus.SecureChannelFailure })
            {
                this.log.Error($"Secure connection could not be established. Please make sure that your application is enabled to use TLS 1.2+. For more information see https://stackoverflow.com/a/58195987/8656352.\n{ex}");
                this.ReInitializeHttpClient();
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured during fetching.\n{ex}");
                this.ReInitializeHttpClient();
            }

            return lastConfig;
        }

        private async ValueTask<Tuple<HttpResponseMessage, string>> FetchRequest(ProjectConfig lastConfig,
            Uri requestUri, bool isAsync, sbyte maxExecutionCount = 3)
        {
            do
            {
                var request = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = requestUri };

                if (lastConfig.HttpETag != null)
                {
                    request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(lastConfig.HttpETag));
                }

                HttpResponseMessage response;
#if NET5_0_OR_GREATER
                response = isAsync
                    ? await this.httpClient.SendAsync(request, this.cancellationTokenSource.Token).ConfigureAwait(false)
                    : this.httpClient.Send(request, this.cancellationTokenSource.Token);
#else
                response = await this.httpClient.SendAsync(request, this.cancellationTokenSource.Token).ConfigureAwait(false);
#endif

                if (response.IsSuccessStatusCode)
                {
#if NET5_0_OR_GREATER
                    var responseBody = isAsync
                        ? await response.Content.ReadAsStringAsync(this.cancellationTokenSource.Token).ConfigureAwait(false)
                        : response.Content.ReadAsString(this.cancellationTokenSource.Token);
#else
                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

                    if (!this.deserializer.TryDeserialize(responseBody, out var body))
                        return Tuple.Create<HttpResponseMessage, string>(response, null);

                    if (body?.Preferences != null)
                    {
                        var newBaseUrl = body.Preferences.Url;

                        if (newBaseUrl == null || requestUri.Host == new Uri(newBaseUrl).Host)
                        {
                            return Tuple.Create(response, responseBody);
                        }

                        RedirectMode redirect = body.Preferences.RedirectMode;

                        if (isCustomUri && redirect != RedirectMode.Force)
                        {
                            return Tuple.Create(response, responseBody);
                        }

                        UpdateRequestUri(new Uri(newBaseUrl));

                        if (redirect == RedirectMode.No)
                        {
                            return Tuple.Create(response, responseBody);
                        }

                        if (redirect == RedirectMode.Should)
                        {
                            this.log.Warning(
                                "Your dataGovernance parameter at ConfigCatClient initialization is not in sync " +
                                "with your preferences on the ConfigCat Dashboard: " +
                                "https://app.configcat.com/organization/data-governance. " +
                                "Only Organization Admins can access this preference.");
                        }

                        if (maxExecutionCount <= 1)
                        {
                            log.Error("Redirect loop during config.json fetch. Please contact support@configcat.com.");
                            return Tuple.Create(response, responseBody);
                        }

                        requestUri = ReplaceUri(request.RequestUri, new Uri(newBaseUrl));
                        continue;
                    }

                    return Tuple.Create(response, responseBody);
                }

                return Tuple.Create<HttpResponseMessage, string>(response, null);
            } while (--maxExecutionCount > 0);

            return Tuple.Create<HttpResponseMessage, string>(null, null);
        }

        private void UpdateRequestUri(Uri newUri)
        {
            lock (this.lck)
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
            lock (this.lck)
            {
                ReInitializeHttpClientLogic();
            }
        }

        private void ReInitializeHttpClientLogic()
        {
            if (this.httpClientHandler == null)
            {
                this.httpClient = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                });
            }
            else
            {
                this.httpClient = new HttpClient(this.httpClientHandler, false);
            }

            this.httpClient.Timeout = this.timeout;
            this.httpClient.DefaultRequestHeaders.Add("X-ConfigCat-UserAgent",
                new ProductInfoHeaderValue("ConfigCat-Dotnet", productVersion).ToString());
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            httpClient?.Dispose();
        }
    }
}