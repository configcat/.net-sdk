using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigCat.Client
{
    internal sealed class HttpConfigFetcher : IConfigFetcher, IDisposable
    {
        private enum RedirectMode : byte
        {
            NoRedirect = 0,
            ShouldRedirect = 1,
            ForceRedirect = 2
        }

        private readonly object lck = new object();
        private readonly object flck = new object();

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
            var newConfig = ProjectConfig.Empty;

            try
            {
                newConfig = lastConfig;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(this.requestUri.ToString())
                };

                var fetchResult = await FetchRequest(lastConfig, request);

                var response = fetchResult.Item1;

                if (response.IsSuccessStatusCode)
                {
                    newConfig.HttpETag = response.Headers.ETag.Tag;

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

        private async Task<Tuple<HttpResponseMessage, string>> FetchRequest(ProjectConfig lastConfig, HttpRequestMessage request, byte maxExecutionCount = 2)
        {
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

                    byte redirect = body.Preferences.RedirectMode;

                    if (isCustomUri && redirect != (byte)RedirectMode.ForceRedirect)
                    {
                        return Tuple.Create(response, responseBody);
                    }

                    UpdateRequestUri(new Uri(newBaseUrl));

                    if (redirect == (byte)RedirectMode.NoRedirect)
                    {
                        return Tuple.Create(response, responseBody);
                    }

                    if (redirect == (byte)RedirectMode.ShouldRedirect)
                    {
                        this.log.Warning("Your dataGovernance parameter at ConfigCatClient initialization is not in sync " +
                                         "with your preferences on the ConfigCat Dashboard: " +
                                         "https://app.configcat.com/organization/data-governance. " +
                                         "Only Organization Admins can access this preference.");
                    }

                    if (maxExecutionCount <= 0)
                    {
                        log.Error("Redirect loop during config.json fetch. Please contact support@configcat.com.");
                        return Tuple.Create(response, responseBody);
                    }

                    return await this.FetchRequest(
                        lastConfig,
                        new HttpRequestMessage
                        {
                            RequestUri = ReplaceUri(request.RequestUri, new Uri(newBaseUrl)),
                            Method = HttpMethod.Get
                        },
                        --maxExecutionCount);
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