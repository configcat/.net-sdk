using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    internal sealed class HttpConfigFetcher : IConfigFetcher
    {
        private readonly object lck = new object();

        private readonly string productVersion;

        private readonly ILogger log;

        private readonly HttpClientHandler httpClientHandler;

        private HttpClient httpClient;

        private readonly Uri requestUri;
               
        public HttpConfigFetcher(Uri requestUri, string productVersion, ILoggerFactory loggerFactory, HttpClientHandler httpClientHandler)
        {
            this.requestUri = requestUri;

            this.productVersion = productVersion;

            this.log = loggerFactory.GetLogger(nameof(HttpConfigFetcher));

            this.httpClientHandler = httpClientHandler;

            ReInitializeHttpClient();
        }

        public async Task<ProjectConfig> Fetch(ProjectConfig lastConfig)
        {
            ProjectConfig newConfig = ProjectConfig.Empty;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = this.requestUri
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
                else
                {
                    this.log.Warning("Unexpected statuscode - " + response.StatusCode);

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

        private void ReInitializeHttpClient()
        {
            lock (this.lck)
            {
                if (this.httpClientHandler == null)
                {
                    this.httpClient = new HttpClient()
                    {
                        Timeout = TimeSpan.FromSeconds(30)
                    };
                }
                else
                {
                    this.httpClient = new HttpClient(this.httpClientHandler)
                    {
                        Timeout = TimeSpan.FromSeconds(30)
                    };
                }

                this.httpClient.DefaultRequestHeaders.Add("X-ConfigCat-UserAgent", new ProductInfoHeaderValue("ConfigCat-Dotnet", productVersion).ToString());
            }
        }
    }
}