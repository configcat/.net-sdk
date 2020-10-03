using System;
using System.Net.Http;

namespace ConfigCat.Client
{
    /// <summary>
    /// Defines base configuration properties
    /// </summary>
    public abstract class ConfigurationBase
    {
        private ILogger _logger;

        /// <summary>
        /// Logger instance
        /// </summary>
        public ILogger Logger
        {
            get
            {
                return this._logger ?? new LoggerWrapper(new ConsoleLogger());
            }
            set
            {
                this._logger = new LoggerWrapper(value ?? throw new ArgumentNullException(nameof(Logger)));
            }
        }

        /// <summary>
        /// SDK Key to get your configuration
        /// </summary>
        public string SdkKey { set; get; }

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
            if (string.IsNullOrEmpty(this.SdkKey))
            {
                throw new ArgumentException("Invalid SDK Key.", nameof(this.SdkKey));
            }

            if (this.Logger == null)
            {
                throw new ArgumentNullException(nameof(this.Logger));
            }
        }

        internal Uri CreateUrl()
        {
            return new Uri(BaseUrl, "configuration-files/" + this.SdkKey + "/" + ConfigFileName);
        }

        internal const string ConfigFileName = "config_v4.json";
    }
}