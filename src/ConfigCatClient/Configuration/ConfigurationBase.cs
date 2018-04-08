using System;
using ConfigCat.Client.Logging;

namespace ConfigCat.Client.Configuration
{
    /// <summary>
    /// Defines base configuration properties
    /// </summary>
    public abstract class ConfigurationBase
    {
        private string projectSecret;

        internal Uri Url { get; private set; }       

        /// <summary>
        /// Factory method of <c>ILogger</c>
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

        /// <summary>
        /// Project secret to get your configuration
        /// </summary>
        public string ProjectSecret
        {
            set
            {
                this.projectSecret = value;

                this.Url = CreateUrl(value);
            }
            get
            {
                return this.projectSecret;
            }
        }

        internal virtual void Validate()
        {
            if (string.IsNullOrEmpty(this.ProjectSecret))
            {
                throw new ArgumentException("Invalid project secret value.", nameof(this.ProjectSecret));
            }

            if (this.LoggerFactory == null)
            {
                throw new ArgumentNullException(nameof(this.LoggerFactory));
            }
        }

        private static Uri CreateUrl(string projectSecret)
        {
            return new Uri("https://cdn.configcat.com/configuration-files/" + projectSecret + "/config.json");
        }
    }
}