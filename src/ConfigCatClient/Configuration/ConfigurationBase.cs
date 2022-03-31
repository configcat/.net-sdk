using ConfigCat.Client.Configuration;
using System;
using System.Net.Http;

namespace ConfigCat.Client
{
    /// <summary>
    /// Defines base configuration properties.
    /// </summary>
    public abstract class ConfigurationBase
    {
        private ILogger logger = new ConsoleLogger(LogLevel.Warning);

        /// <summary>
        /// Logger instance.
        /// </summary>
        public ILogger Logger
        {
            get => this.logger;
            set
            {
                this.logger = value ?? throw new ArgumentNullException(nameof(Logger));
            }
        }

        /// <summary>
        /// SDK Key to get your configuration.
        /// </summary>
        public string SdkKey { set; get; }

        /// <summary>
        /// If you want to use custom caching instead of the client's default InMemoryConfigCache, You can provide an implementation of IConfigCache.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public IConfigCache ConfigCache { get; set; } // Backward compatibility, it'll be changed to IConfigCatCache later.
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// HttpClientHandler to provide network credentials and proxy settings.
        /// </summary>
        public HttpClientHandler HttpClientHandler { get; set; }

        /// <summary>
        /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat.
        /// </summary>
        public Uri BaseUrl { get; set; } = BaseUrlGlobal;

        /// <summary>
        /// Default: Global. Set this parameter to be in sync with the Data Governance preference on the Dashboard:
        /// https://app.configcat.com/organization/data-governance (Only Organization Admins have access)
        /// </summary>
        public DataGovernance DataGovernance { get; set; } = DataGovernance.Global;

        /// <summary>
        /// Timeout for underlying http calls. Defaults to 30 seconds.
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Feature flag and setting overrides.
        /// </summary>
        public FlagOverrides FlagOverrides { get; set; }

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
                baseUri = DataGovernance switch
                {
                    DataGovernance.EuOnly => BaseUrlEu,
                    _ => BaseUrlGlobal,
                };
            }

            return new Uri(baseUri, "configuration-files/" + this.SdkKey + "/" + ConfigFileName);
        }

        internal const string ConfigFileName = "config_v5.json";

        internal static readonly Uri BaseUrlGlobal = new("https://cdn-global.configcat.com");

        internal static readonly Uri BaseUrlEu = new("https://cdn-eu.configcat.com");

        internal bool IsCustomBaseUrl => BaseUrl != BaseUrlGlobal && BaseUrl != BaseUrlEu;

        // Remove this helper when we delete the obsolate client constructors.
        internal void ToOptions(ConfigCatClientOptions options)
        {
            options.Logger = this.Logger;
            options.HttpClientHandler = this.HttpClientHandler;
            options.SdkKey = this.SdkKey;
            options.DataGovernance = this.DataGovernance;
            options.ConfigCache = this.ConfigCache;
            options.BaseUrl = this.BaseUrl;
        }
    }
}