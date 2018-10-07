using System;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json.Linq;
using ConfigCat.Client.Logging;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Evaluate;

namespace ConfigCat.Client
{
    /// <summary>
    /// Client for ConfigCat platform
    /// </summary>
    public class ConfigCatClient : IConfigCatClient
    {
        private ILogger log;

        private readonly IConfigService configService;

        private readonly IConfigEvaluator configEvaluator;

        private static readonly string version = typeof(ConfigCatClient).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        /// <summary>
        /// Create an instance of ConfigCatClient and setup AutoPoll mode
        /// </summary>
        /// <param name="apiKey">Api key to access configuration</param>
        /// <exception cref="ArgumentException">When the <paramref name="apiKey"/> is null or empty</exception>                
        public ConfigCatClient(string apiKey) : this(new AutoPollConfiguration { ApiKey = apiKey })
        {
        }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup AutoPoll mode
        /// </summary>
        /// <param name="configuration">Configuration for AutoPolling mode</param>
        /// <exception cref="ArgumentException">When the configuration contains any invalid property</exception>
        /// <exception cref="ArgumentNullException">When the configuration is null</exception>                
        public ConfigCatClient(AutoPollConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.Validate();

            InitializeClient(configuration);

            var configService = new AutoPollConfigService(
                    new HttpConfigFetcher(configuration.Url, "a-" + version, configuration.LoggerFactory),
                    new InMemoryConfigCache(),
                    TimeSpan.FromSeconds(configuration.PollIntervalSeconds),
                    TimeSpan.FromSeconds(configuration.MaxInitWaitTimeSeconds),
                    configuration.LoggerFactory);

            configService.OnConfigurationChanged += configuration.RaiseOnConfigurationChanged;

            this.configService = configService;
        }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup LazyLoad mode
        /// </summary>
        /// <param name="configuration">Configuration for LazyLoading mode</param>
        /// <exception cref="ArgumentException">When the configuration contains any invalid property</exception>
        /// <exception cref="ArgumentNullException">When the configuration is null</exception>  
        public ConfigCatClient(LazyLoadConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.Validate();

            InitializeClient(configuration);

            var configService = new LazyLoadConfigService(
                new HttpConfigFetcher(configuration.Url, "l-" + version, configuration.LoggerFactory),
                new InMemoryConfigCache(),
                configuration.LoggerFactory,
                TimeSpan.FromSeconds(configuration.CacheTimeToLiveSeconds));

            this.configService = configService;
        }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup ManualPoll mode
        /// </summary>
        /// <param name="configuration">Configuration for LazyLoading mode</param>
        /// <exception cref="ArgumentException">When the configuration contains any invalid property</exception>
        /// <exception cref="ArgumentNullException">When the configuration is null</exception>  
        public ConfigCatClient(ManualPollConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.Validate();

            InitializeClient(configuration);

            var configService = new ManualPollConfigService(
                new HttpConfigFetcher(configuration.Url, "m-" + version, configuration.LoggerFactory),
                new InMemoryConfigCache(),
                configuration.LoggerFactory);

            this.configService = configService;
        }

        private void InitializeClient(ConfigurationBase configuration)
        {
            this.log = configuration.LoggerFactory.GetLogger(nameof(ConfigCatClient));
        }

        /// <inheritdoc />
        public string GetConfigurationJsonString()
        {
            var c = this.configService.GetConfigAsync().Result;

            return c.JsonString;
        }

        /// <inheritdoc />
        public async Task<string> GetConfigurationJsonStringAsync()
        {
            var c = await this.configService.GetConfigAsync();

            return c.JsonString;
        }

        /// <inheritdoc />
        public T GetValue<T>(string key, T defaultValue, User user = null)
        {
            try
            {
                var c = this.configService.GetConfigAsync().Result;

                return this.configEvaluator.GetValue<T>(c, key, defaultValue, user);                
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetValue' method.\n{ex.ToString()}");

                return defaultValue;
            }
        }

        /// <inheritdoc />
        public async Task<T> GetValueAsync<T>(string key, T defaultValue, User user = null)
        {
            try
            {
                var c = await this.configService.GetConfigAsync().ConfigureAwait(false);

                return this.configEvaluator.GetValue<T>(c, key, defaultValue, user);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetValueAsync' method.\n{ex.ToString()}");

                return defaultValue;
            }
        }

        /// <inheritdoc />
        public T GetConfiguration<T>(T defaultValue)
        {
            try
            {
                var c = this.configService.GetConfigAsync().Result;

                return GetConfigurationLogic<T>(c, defaultValue);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetConfiguration' method.\n{ex.ToString()}");

                return defaultValue;
            }
        }

        /// <inheritdoc />
        public async Task<T> GetConfigurationAsync<T>(T defaultValue)
        {
            try
            {
                var c = await this.configService.GetConfigAsync().ConfigureAwait(false);

                return GetConfigurationLogic<T>(c, defaultValue);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetConfigurationAsync' method.\n{ex.ToString()}");

                return defaultValue;
            }
        }

        /// <inheritdoc />
        public void ForceRefresh()
        {
            this.configService.RefreshConfigAsync().Wait();
        }

        /// <inheritdoc />
        public async Task ForceRefreshAsync()
        {
            await this.configService.RefreshConfigAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.configService != null && this.configService is IDisposable)
            {
                ((IDisposable)this.configService).Dispose();
            }
        }

        private T GetValueLogic<T>(ProjectConfig config, string key, T defaultValue)
        {
            if (config.JsonString == null)
            {
                this.log.Warning("ConfigJson is not present, returning defaultValue");

                return defaultValue;
            }

            var json = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(config.JsonString);

            var rawValue = json.GetValue(key);

            if (rawValue == null)
            {
                this.log.Warning($"Unknown key: '{key}'");

                return defaultValue;
            }

            return rawValue.Value<T>();
        }

        private T GetConfigurationLogic<T>(ProjectConfig config, T defaultValue)
        {
            if (config.JsonString == null)
            {
                return defaultValue;
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(config.JsonString);
        }

        /// <summary>
        /// Create a <see cref="ConfigCatClientBuilder"/> instance to setup the client
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static ConfigCatClientBuilder Create(string apiKey)
        {
            return ConfigCatClientBuilder.Initialize(apiKey);
        }
    }
}