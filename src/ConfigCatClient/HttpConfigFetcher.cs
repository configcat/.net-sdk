using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    internal sealed class HttpConfigFetcher : IConfigFetcher, IDisposable
    {
        private Uri requestBaseUriEffective;

        private readonly object lck = new object();

        private readonly Uri requestBaseUriFallback;

        private readonly string configFileRelativeUrl;

        private readonly IConfigDeserializer configDeserializer;

        private readonly string productVersion;

        private readonly ILogger log;

        private readonly HttpClientHandler httpClientHandler;

        private HttpClient httpClient;


        public HttpConfigFetcher(Uri requestBaseUri, string configFileRelativeUrl, IConfigDeserializer configDeserializer, string productVersion, ILogger logger, HttpClientHandler httpClientHandler)
        {
            this.requestBaseUriFallback = requestBaseUri;

            this.configFileRelativeUrl = configFileRelativeUrl;

            this.configDeserializer = configDeserializer;

            this.productVersion = productVersion;

            this.log = logger;

            this.httpClientHandler = httpClientHandler;

            ReInitializeHttpClient();
            SetRequestBaseUrlFromProjectConfig(ProjectConfig.Empty);
        }

        public async Task<ProjectConfig> Fetch(ProjectConfig lastConfig)
        {
            ProjectConfig newConfig = ProjectConfig.Empty;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(this.requestBaseUriEffective, this.configFileRelativeUrl),
            };

            if (lastConfig.HttpETag != null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(lastConfig.HttpETag));
            }

            try
            {
                var response = await this.httpClient.SendAsync(request).ConfigureAwait(false);

                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    newConfig = lastConfig;
                }
                else if (response.IsSuccessStatusCode)
                {
                    newConfig.HttpETag = response.Headers.ETag.Tag;

                    newConfig.JsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    newConfig = lastConfig;

                    this.log.Error("Double-check your API KEY at https://app.configcat.com/apikey");
                }
                else
                {
                    this.ReInitializeHttpClient();
                }

                this.SetRequestBaseUrlFromProjectConfig(newConfig);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'Fetch' method.\n{ex}");

                this.ReInitializeHttpClient();
            }

            newConfig.TimeStamp = DateTime.UtcNow;

            return newConfig;
        }

        private void ReInitializeHttpClient()
        {
            lock (this.lck)
            {
                if (this.httpClientHandler == null)
                {
                    this.httpClient = new HttpClient(new HttpClientHandler
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                    });
                }
                else
                {
                    this.httpClient = new HttpClient(this.httpClientHandler, false);
                }

                this.httpClient.Timeout = TimeSpan.FromSeconds(30);

                this.httpClient.DefaultRequestHeaders.Add("X-ConfigCat-UserAgent", new ProductInfoHeaderValue("ConfigCat-Dotnet", this.productVersion).ToString());
            }
        }

        private void SetRequestBaseUrlFromProjectConfig(ProjectConfig projectConfig)
        {
            if (projectConfig.Equals(ProjectConfig.Empty))
            {
                this.requestBaseUriEffective = this.requestBaseUriFallback;
                return;
            }

            if (projectConfig.JsonString == null)
            {
                this.requestBaseUriEffective = this.requestBaseUriFallback;
                return;
            }

            if (!this.configDeserializer.TryDeserialize(projectConfig, out var settings))
            {
                this.requestBaseUriEffective = this.requestBaseUriFallback;
                return;
            }

            if (settings.ServiceSpaceSettings == null)
            {
                this.requestBaseUriEffective = this.requestBaseUriFallback;
                return;
            }

            if (string.IsNullOrEmpty(settings.ServiceSpaceSettings.CdnBaseUrl))
            {
                this.requestBaseUriEffective = this.requestBaseUriFallback;
                return;
            }

            if (!settings.ServiceSpaceSettings.CdnBaseUrl.StartsWith("https://"))
            {
                this.requestBaseUriEffective = this.requestBaseUriFallback;
                return;
            }

            if (!settings.ServiceSpaceSettings.CdnBaseUrl.EndsWith("configcat.com"))
            {
                this.requestBaseUriEffective = this.requestBaseUriFallback;
                return;
            }

            this.requestBaseUriEffective = new Uri(settings.ServiceSpaceSettings.CdnBaseUrl);
        }

        public void Dispose()
        {
            if (this.httpClient != null)
            {
                this.httpClient.Dispose();
            }
        }
    }

    internal sealed class WrapClientHandler : DelegatingHandler { }
}