using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluate;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    /// <summary>
    /// Client for ConfigCat platform
    /// </summary>
    public class ConfigCatClient : IConfigCatClient
    {
        private readonly ILogger log;

        private readonly IRolloutEvaluator configEvaluator;

        private readonly IConfigService configService;

        private readonly IConfigDeserializer configDeserializer;

        private static readonly string version = typeof(ConfigCatClient).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        /// <inheritdoc />
        public LogLevel LogLevel
        {
            get => this.log.LogLevel;
            set => this.log.LogLevel = value;
        }

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
            : this((ConfigurationBase)configuration)
        {
            var configService = new AutoPollConfigService(
                    new HttpConfigFetcher(
                        configuration.BaseUrl,
                        configuration.CreateConfigRelativeUrl(),
                        this.configDeserializer,
                        "a-" + version,
                        configuration.Logger,
                        configuration.HttpClientHandler),
                    configuration.ConfigCache ?? new InMemoryConfigCache(),
                    TimeSpan.FromSeconds(configuration.PollIntervalSeconds),
                    TimeSpan.FromSeconds(configuration.MaxInitWaitTimeSeconds),
                    configuration.Logger);

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
            : this((ConfigurationBase)configuration)
        {
            var configService = new LazyLoadConfigService(
                    new HttpConfigFetcher(
                        configuration.BaseUrl,
                        configuration.CreateConfigRelativeUrl(),
                        this.configDeserializer,
                        "l-" + version,
                        configuration.Logger,
                        configuration.HttpClientHandler),
                    configuration.ConfigCache ?? new InMemoryConfigCache(),
                    configuration.Logger,
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
            : this((ConfigurationBase)configuration)
        {
            var configService = new ManualPollConfigService(
                    new HttpConfigFetcher(
                        configuration.BaseUrl,
                        configuration.CreateConfigRelativeUrl(),
                        this.configDeserializer,
                        "m-" + version,
                        configuration.Logger,
                        configuration.HttpClientHandler),
                    configuration.ConfigCache ?? new InMemoryConfigCache(),
                    configuration.Logger);

            this.configService = configService;
        }

        private ConfigCatClient(ConfigurationBase configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.Validate();

            this.log = configuration.Logger;
            this.configDeserializer = new ConfigDeserializer(this.log);
            this.configEvaluator = new RolloutEvaluator(this.log, this.configDeserializer);
        }

        /// <summary>
        /// For test purpose only
        /// </summary>        
        internal ConfigCatClient(IConfigService configService, ILogger logger, IRolloutEvaluator evaluator, IConfigDeserializer configDeserializer)
        {
            this.configService = configService;
            this.log = logger;
            this.configEvaluator = evaluator;
            this.configDeserializer = configDeserializer;
        }

        /// <inheritdoc />
        public T GetValue<T>(string key, T defaultValue, User user = null)
        {
            try
            {
                var c = this.configService.GetConfigAsync().Result;

                return this.configEvaluator.Evaluate<T>(c, key, defaultValue, user);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetValue' method.\n{ex}");

                return defaultValue;
            }
        }

        /// <inheritdoc />
        public async Task<T> GetValueAsync<T>(string key, T defaultValue, User user = null)
        {
            try
            {
                var c = await this.configService.GetConfigAsync().ConfigureAwait(false);

                return this.configEvaluator.Evaluate<T>(c, key, defaultValue, user);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetValueAsync' method.\n{ex}");

                return defaultValue;
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            try
            {
                return this.GetAllKeysAsync().Result;
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllKeys' method.\n{ex}");
                return new string[0];
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAllKeysAsync()
        {
            try
            {
                var c = await this.configService.GetConfigAsync().ConfigureAwait(false);
                if (this.configDeserializer.TryDeserialize(c, out var settings))
                {
                    return settings.UserSpaceSettings.Keys;
                }

                this.log.Warning("Config deserialization failed.");
                return new string[0];
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllKeysAsync' method.\n{ex}");
                return new string[0];
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