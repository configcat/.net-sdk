using System;
using System.Net.Http;

namespace ConfigCat.Client
{
    /// <summary>
    /// Defines base configuration properties
    /// </summary>
    public abstract class ConfigurationBase
    {
        /// <summary>
        /// Factory method of <c>ILogger</c>
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

        /// <summary>
        /// Api key to get your configuration
        /// </summary>
        public string ApiKey { set; get; }

        /// <summary>
        /// If you want to use custom caching instead of the client's default InMemoryConfigCache, You can provide an implementation of IConfigCache.
        /// </summary>
        public IConfigCache ConfigCache { get; set; }

        /// <summary>
        /// HttpClientHandler to provide network credentials and proxy settings
        /// </summary>
        public HttpClientHandler HttpClientHandler { get; set; } 

        /// <summary>
        /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat
        /// </summary>
        public Uri BaseUrl { get; set; } = new Uri("https://cdn.configcat.com");

        internal virtual void Validate()
        {
            if (string.IsNullOrEmpty(this.ApiKey))
            {
                throw new ArgumentException("Invalid api key value.", nameof(this.ApiKey));
            }

            if (this.LoggerFactory == null)
            {
                throw new ArgumentNullException(nameof(this.LoggerFactory));
            }
        }

        internal Uri CreateUrl()
        {
            return new Uri(BaseUrl, "configuration-files/" + this.ApiKey + "/config_v2.json");
        }
    }
}