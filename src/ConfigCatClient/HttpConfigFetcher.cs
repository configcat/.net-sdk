using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluate;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client
{
    internal sealed class HttpConfigFetcher : IConfigFetcher, IDisposable
    {
        private readonly object lck = new();
        private readonly string productVersion;
        private readonly ILogger log;

        private readonly HttpClientHandler httpClientHandler;
        private readonly IConfigDeserializer deserializer;
        private readonly bool isCustomUri;

        private HttpClient httpClient;

        private Uri requestUri;

        public HttpConfigFetcher(Uri requestUri, string productVersion, ILogger logger,
            HttpClientHandler httpClientHandler, IConfigDeserializer deserializer, bool isCustomUri)
        {
            this.requestUri = requestUri;
            this.productVersion = productVersion;
            this.log = logger;
            this.httpClientHandler = httpClientHandler;
            this.deserializer = deserializer;
            this.isCustomUri = isCustomUri;

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
            var newConfig = lastConfig;

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
                    newConfig.HttpETag = response.Headers.ETag?.Tag;
                    newConfig.JsonString = fetchResult.Item2;
                }
                else
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
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'Fetch' method.\n{ex}");

                this.ReInitializeHttpClient();
            }

            newConfig.TimeStamp = DateTime.UtcNow;
            return newConfig;
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

#if NET5_0_OR_GREATER
                var response = isAsync
                    ? await this.httpClient.SendAsync(request).ConfigureAwait(false)
                    : this.httpClient.Send(request);
#else
                var response = await this.httpClient.SendAsync(request).ConfigureAwait(false);
#endif

                if (response.IsSuccessStatusCode)
                {
#if NET5_0_OR_GREATER
                    var responseBody = isAsync
                        ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                        : response.Content.ReadAsString();
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

            this.httpClient.Timeout = TimeSpan.FromSeconds(30);
            this.httpClient.DefaultRequestHeaders.Add("X-ConfigCat-UserAgent",
                new ProductInfoHeaderValue("ConfigCat-Dotnet", productVersion).ToString());
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}