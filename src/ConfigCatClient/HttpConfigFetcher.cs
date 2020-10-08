using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluate;
using Newtonsoft.Json;

namespace ConfigCat.Client
{
    internal sealed class HttpConfigFetcher : IConfigFetcher, IDisposable
    {
        private readonly object lck = new object();

        private readonly string productVersion;

        private readonly ILogger log;

        private readonly HttpClientHandler httpClientHandler;

        private readonly bool isCustomUri;

        private HttpClient httpClient;

        private Uri requestUri;

        public HttpConfigFetcher(Uri requestUri, string productVersion, ILogger logger, HttpClientHandler httpClientHandler, bool isCustomUri)
        {
            this.requestUri = requestUri;

            this.productVersion = productVersion;

            this.log = logger;

            this.httpClientHandler = httpClientHandler;

            this.isCustomUri = isCustomUri;

            ReInitializeHttpClient();
        }

        public async Task<ProjectConfig> Fetch(ProjectConfig lastConfig)
        {
            var newConfig = lastConfig;

            try
            {
                var fetchResult = await FetchRequest(lastConfig, this.requestUri);

                var response = fetchResult.Item1;

                if (response.IsSuccessStatusCode)
                {
                    newConfig.HttpETag = response.Headers.ETag?.Tag;

                    newConfig.JsonString = fetchResult.Item2;
                }
                else if (response.StatusCode == HttpStatusCode.NotModified)
                {
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    this.log.Error("Double-check your SDK Key at https://app.configcat.com/sdkkey");
                }
                else
                {
                    this.ReInitializeHttpClient();
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

        private async Task<Tuple<HttpResponseMessage, string>> FetchRequest(ProjectConfig lastConfig, Uri requestUri, sbyte maxExecutionCount = 3)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = requestUri
            };

            if (lastConfig.HttpETag != null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(lastConfig.HttpETag));
            }

            var response = await this.httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                var body = JsonConvert.DeserializeObject<SettingsWithPreferences>(responseBody);

                if (body?.Preferences != null)
                {
                    var newBaseUrl = body.Preferences.Url;

                    if (newBaseUrl == null || requestUri.Host == new Uri(newBaseUrl).Host)
                    {
                        return Tuple.Create(response, responseBody);
                    }

                    Evaluate.RedirectMode redirect = body.Preferences.RedirectMode;

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
                        this.log.Warning("Your dataGovernance parameter at ConfigCatClient initialization is not in sync " +
                                         "with your preferences on the ConfigCat Dashboard: " +
                                         "https://app.configcat.com/organization/data-governance. " +
                                         "Only Organization Admins can access this preference.");
                    }

                    if (maxExecutionCount <= 1)
                    {
                        log.Error("Redirect loop during config.json fetch. Please contact support@configcat.com.");
                        return Tuple.Create(response, responseBody);
                    }

                    return await this.FetchRequest(
                        lastConfig,
                        ReplaceUri(request.RequestUri, new Uri(newBaseUrl)),
                        --maxExecutionCount);
                }
                else
                {
                    return Tuple.Create(response, responseBody);
                }
            }

            return Tuple.Create<HttpResponseMessage, string>(response, null);
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

            this.httpClient.DefaultRequestHeaders.Add("X-ConfigCat-UserAgent", new ProductInfoHeaderValue("ConfigCat-Dotnet", productVersion).ToString());
        }

        public void Dispose()
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
            }
        }
    }
}