using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Security;
using Newtonsoft.Json;

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

        private readonly CacheParameters cacheParameters;

        private static readonly string version = typeof(ConfigCatClient).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        /// <inheritdoc />
        public LogLevel LogLevel
        {
            get
            {
                return log.LogLevel;
            }
            set
            {
                log.LogLevel = value;
            }
        }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup AutoPoll mode
        /// </summary>
        /// <param name="sdkKey">SDK Key to access configuration</param>
        /// <param name="dataGovernance">Default: Global. Set this parameter to be in sync with the Data Governance preference on the Dashboard: https://app.configcat.com/organization/data-governance (Only Organization Admins have access)</param>
        /// <exception cref="ArgumentException">When the <paramref name="sdkKey"/> is null or empty</exception>                
        public ConfigCatClient(string sdkKey, DataGovernance dataGovernance = DataGovernance.Global) : this(new AutoPollConfiguration { SdkKey = sdkKey, DataGovernance = dataGovernance })
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
            var autoPollService = new AutoPollConfigService(
                   new HttpConfigFetcher(configuration.CreateUri(), "a-" + version, configuration.Logger, configuration.HttpClientHandler, this.configDeserializer, configuration.IsCustomBaseUrl),
                   this.cacheParameters,
                   TimeSpan.FromSeconds(configuration.PollIntervalSeconds),
                   TimeSpan.FromSeconds(configuration.MaxInitWaitTimeSeconds),
                   configuration.Logger
                   );

            autoPollService.OnConfigurationChanged += configuration.RaiseOnConfigurationChanged;

            this.configService = autoPollService;
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
            var lazyLoadService = new LazyLoadConfigService(
               new HttpConfigFetcher(configuration.CreateUri(), "l-" + version, configuration.Logger, configuration.HttpClientHandler, this.configDeserializer, configuration.IsCustomBaseUrl),
               this.cacheParameters,
               configuration.Logger,
               TimeSpan.FromSeconds(configuration.CacheTimeToLiveSeconds));

            this.configService = lazyLoadService;
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
                new HttpConfigFetcher(configuration.CreateUri(), "m-" + version, configuration.Logger, configuration.HttpClientHandler, this.configDeserializer, configuration.IsCustomBaseUrl),
                this.cacheParameters,
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
            this.configDeserializer = new ConfigDeserializer(this.log, JsonSerializer.Create());
            this.configEvaluator = new RolloutEvaluator(this.log, this.configDeserializer);
            this.cacheParameters = new CacheParameters
            {
                ConfigCache = configuration.ConfigCache ?? new InMemoryConfigCache(),
                CacheKey = GetCacheKey(configuration)
            };
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
                var config = this.configService.GetConfigAsync().GetAwaiter().GetResult();

                return this.configEvaluator.Evaluate<T>(config, key, defaultValue, user);
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
                var config = await this.configService.GetConfigAsync().ConfigureAwait(false);

                return this.configEvaluator.Evaluate<T>(config, key, defaultValue, user);
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
                var config = this.configService.GetConfigAsync().GetAwaiter().GetResult();

                if (this.configDeserializer.TryDeserialize(config.JsonString, out var settings)) return settings.Settings.Keys;

                this.log.Warning("Config deserialization failed.");

                return new string[0];
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
                var config = await this.configService.GetConfigAsync().ConfigureAwait(false);

                if (this.configDeserializer.TryDeserialize(config.JsonString, out var settings)) return settings.Settings.Keys;

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
            try
            {
                this.configService.RefreshConfigAsync().GetAwaiter().GetResult();                
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'ForceRefresh' method.\n{ex}");
            }
        }

        /// <inheritdoc />
        public async Task ForceRefreshAsync()
        {
            try
            {
                await this.configService.RefreshConfigAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'ForceRefreshAsync' method.\n{ex}");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.configService != null && this.configService is IDisposable)
            {
                ((IDisposable)this.configService).Dispose();
            }
        }

        /// <inheritdoc />
        public string GetVariationId(string key, string defaultVariationId, User user = null)
        {
            try
            {
                var config = this.configService.GetConfigAsync().GetAwaiter().GetResult();

                return this.configEvaluator.EvaluateVariationId(config, key, defaultVariationId, user);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetVariationId' method.\n{ex}");

                return defaultVariationId;
            }
        }

        /// <inheritdoc />
        public async Task<string> GetVariationIdAsync(string key, string defaultVariationId, User user = null)
        {
            try
            {
                var c = await this.configService.GetConfigAsync().ConfigureAwait(false);

                return this.configEvaluator.EvaluateVariationId(c, key, defaultVariationId, user);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetVariationIdAsync' method.\n{ex}");

                return defaultVariationId;
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllVariationId(User user = null)
        {
            try
            {
                var config = this.configService.GetConfigAsync().GetAwaiter().GetResult();

                return GetAllVariationIdLogic(config, user);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllVariationId' method.\n{ex}");
            }

            return Enumerable.Empty<string>();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAllVariationIdAsync(User user = null)
        {
            try
            {
                var config = await this.configService.GetConfigAsync().ConfigureAwait(false);

                return GetAllVariationIdLogic(config, user);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllVariationIdAsync' method.\n{ex}");
            }

            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> GetAllVariationIdLogic(ProjectConfig config, User user)
        {
            if (this.configDeserializer.TryDeserialize(config.JsonString, out var settings))
            {
                var result = new List<string>(settings.Settings.Keys.Count);

                foreach (var key in settings.Settings.Keys)
                {
                    var r = this.configEvaluator.EvaluateVariationId(config, key, null, user);

                    if (r != null)
                    {
                        result.Add(r);
                    }
                }

                return result;
            }

            this.log.Warning("Config deserialization failed.");

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Create a <see cref="ConfigCatClientBuilder"/> instance to setup the client
        /// </summary>
        /// <param name="sdkKey"></param>
        /// <returns></returns>
        public static ConfigCatClientBuilder Create(string sdkKey)
        {
            return ConfigCatClientBuilder.Initialize(sdkKey);
        }

        private static string GetCacheKey(ConfigurationBase configuration)
        {
            var key = $"dotnet_{ConfigurationBase.ConfigFileName}_{configuration.SdkKey}";

            return HashUtils.HashString(key);
        }
    }
}