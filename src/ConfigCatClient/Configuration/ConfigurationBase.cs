using System;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace ConfigCat.Client
{
    /// <summary>
    /// Defines base configuration properties
    /// </summary>
    public abstract class ConfigurationBase
    {
        private ILogger logger;
        
        private Uri baseUrl = new Uri(BaseUrlGlobal);

        /// <summary>
        /// Logger instance
        /// </summary>
        public ILogger Logger
        {
            get
            {
                return this.logger ?? new LoggerWrapper(new ConsoleLogger());
            }
            set
            {
                this.logger = new LoggerWrapper(value ?? throw new ArgumentNullException(nameof(Logger)));
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
        public Uri BaseUrl
        {
            get => baseUrl;
            set
            {
                this.IsCustomBaseUrl = true;
                baseUrl = value;
            }
        }

        /// <summary>
        /// Default: Global. Set this parameter to be in sync with the Data Governance preference on the Dashboard:
        /// https://app.configcat.com/organization/data-governance (Only Organization Admins have access)
        /// </summary>
        public DataGovernance DataGovernance { get; set; } = DataGovernance.Global;

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

        internal Uri CreateUri()
        {
            var baseUri = BaseUrl;

            if (!IsCustomBaseUrl)
            {
                switch (DataGovernance)
                {
                    case DataGovernance.EuOnly:
                        baseUri = new Uri(BaseUrlEu);
                        break;
                    default:
                        baseUri = new Uri(BaseUrlGlobal);
                        break;
                }
            }

            return new Uri(baseUri, "configuration-files/" + this.SdkKey + "/" + ConfigFileName);
        }

        internal const string ConfigFileName = "config_v5.json";

        internal const string BaseUrlGlobal = "https://cdn-global.configcat.com";

        internal const string BaseUrlEu = "https://cdn-eu.configcat.com";

        internal bool IsCustomBaseUrl { get; private set; }
    }
}