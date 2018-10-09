using System;
using ConfigCat.Client.Logging;

namespace ConfigCat.Client.Configuration
{
    /// <summary>
    /// Defines base configuration properties
    /// </summary>
    public abstract class ConfigurationBase
    {
        private string apiKey;

        internal Uri Url { get; private set; }       

        /// <summary>
        /// Factory method of <c>ILogger</c>
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

        /// <summary>
        /// Api key to get your configuration
        /// </summary>
        public string ApiKey
        {
            set
            {
                this.apiKey = value;

                this.Url = CreateUrl(value);
            }
            get
            {
                return this.apiKey;
            }
        }

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

        private static Uri CreateUrl(string apiKey)
        {
            return new Uri("https://cdn.configcat.com/configuration-files/" + apiKey + "/config_v2.json");
        }
    }
}