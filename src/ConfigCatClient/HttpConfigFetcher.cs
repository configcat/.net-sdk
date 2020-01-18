using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    internal sealed class HttpConfigFetcher : IConfigFetcher, IDisposable
    {
        private const string FallbackBaseUrl = "https://cdn.configcat.com";

        private readonly object lck = new object();

        private readonly Uri[] fallbackUris;

        private Uri[] potentialUris;

        private readonly string configFileRelativeUrl;

        private readonly IConfigDeserializer configDeserializer;

        private readonly string productVersion;

        private readonly ILogger log;

        private readonly HttpClientHandler httpClientHandler;

        private HttpClient httpClient;

        private readonly bool isCdnAllowedToOverrideUri;

        private readonly Random rnd = new Random();

        public HttpConfigFetcher(
                    Uri userDefinedBaseUri,
                    string configFileRelativeUrl,
                    string productVersion,
                    IConfigDeserializer configDeserializer,
                    ILogger logger,
                    HttpClientHandler httpClientHandler)
        {
            if(userDefinedBaseUri != null)
            {
                // userDefinedBaseUri is set => the user's goal is to have explicit control over the CDN server's uri
                this.isCdnAllowedToOverrideUri = false;
                this.fallbackUris = new Uri[] { userDefinedBaseUri };
            }
            else
            {
                this.isCdnAllowedToOverrideUri = true;
                this.fallbackUris = new Uri[] { new Uri(FallbackBaseUrl) };
            }

            this.potentialUris = this.fallbackUris;

            this.configFileRelativeUrl = configFileRelativeUrl;

            this.productVersion = productVersion;

            this.configDeserializer = configDeserializer;

            this.log = logger;

            this.httpClientHandler = httpClientHandler;

            ReInitializeHttpClient();
            GetPotentialUris(ProjectConfig.Empty);
        }

        /// <summary>
        /// Returns an optimal request Uri from the list of potential Uris.
        /// </summary>
        private Uri GetActualRequestBaseUri()
        {
            if (this.potentialUris.Length == 1)
                return this.potentialUris[0];

            var idx = this.rnd.Next(0, this.potentialUris.Length);

            return this.potentialUris[idx];
        }

        public async Task<ProjectConfig> Fetch(ProjectConfig lastConfig)
        {
            this.log.Debug($"Fetching configuration starts");

            ProjectConfig newConfig = ProjectConfig.Empty;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(this.GetActualRequestBaseUri(), this.configFileRelativeUrl),
            };
            this.log.Debug($"Request url is '{request.RequestUri}");

            if (lastConfig.HttpETag != null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(lastConfig.HttpETag));
                this.log.Debug($"Request etag is '{lastConfig.HttpETag}");
            }

            try
            {
                this.log.Debug($"Http request sent");
                var response = await this.httpClient.SendAsync(request).ConfigureAwait(false);
                this.log.Debug($"Http response received, status code: {response.StatusCode}, etag: {response.Headers.ETag.Tag}");

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

                if (this.isCdnAllowedToOverrideUri)
                {
                    this.potentialUris = this.GetPotentialUris(newConfig);
                    this.log.Debug($"The new potential CDN url list is: {string.Join<Uri>(", ", this.potentialUris )}");
                }
            }
            catch (Exception ex)
            {
                this.log.Error($"Error in 'HttpConfigFetcher.Fetch()' method. Exception: '{ex}'");
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

        private Uri[] GetPotentialUris(ProjectConfig projectConfig)
        {
            this.log.Debug($"Reading CDN server names from '{this.configFileRelativeUrl}'");

            if (projectConfig.Equals(ProjectConfig.Empty) || projectConfig.JsonString == null)
            {
                this.log.Debug($"Cannot read CDN server names #1");
                return this.fallbackUris;
            }

            if (!this.configDeserializer.TryDeserialize(projectConfig, out var settings))
            {
                this.log.Debug($"Cannot read CDN server names #2");
                return this.fallbackUris;
            }

            if (settings.ServiceSpaceSettings == null || string.IsNullOrEmpty(settings.ServiceSpaceSettings.CdnServerNamesCsv))
            {
                this.log.Debug($"Cannot read CDN server names #3");
                return this.fallbackUris;
            }

            try
            {
                this.log.Debug($"CDN server names found': '{settings.ServiceSpaceSettings.CdnServerNamesCsv}'");
                var urisInConfig = settings.ServiceSpaceSettings.CdnServerNamesCsv
                                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(name => name.Trim())
                                        .Select(name => new Uri($"https://cdn-{name}.configcat.com"))
                                        .ToArray();
                return urisInConfig;
            }
            catch(Exception ex)
            {
                this.log.Error($"Cannot re: {settings.ServiceSpaceSettings.CdnServerNamesCsv}. Exception: {ex}");
                return this.fallbackUris;
            }
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