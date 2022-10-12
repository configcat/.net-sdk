using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Override;

namespace ConfigCat.Client
{
    /// <summary>
    /// Client for ConfigCat platform
    /// </summary>
    public sealed class ConfigCatClient : IConfigCatClient
    {
        private readonly string sdkKey;
        // TODO: Remove this field when we delete the obsolete client constructors.
        private readonly bool isUncached;
        private readonly LoggerWrapper log;
        private readonly IRolloutEvaluator configEvaluator;
        private readonly IConfigService configService;
        private readonly IConfigDeserializer configDeserializer;
        private readonly IOverrideDataSource overrideDataSource;
        private readonly OverrideBehaviour? overrideBehaviour;
        private User defaultUser;

        private static readonly string Version = typeof(ConfigCatClient).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        private static readonly ConfigCatClientCache Instances = new ConfigCatClientCache();

        /// <inheritdoc />
        public LogLevel LogLevel
        {
            get => log.LogLevel;
            set => log.LogLevel = value;
        }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup AutoPoll mode
        /// </summary>
        /// <param name="sdkKey">SDK Key to access configuration</param>
        /// <param name="dataGovernance">Default: Global. Set this parameter to be in sync with the Data Governance preference on the Dashboard: https://app.configcat.com/organization/data-governance (Only Organization Admins have access)</param>
        /// <exception cref="ArgumentException">When the <paramref name="sdkKey"/> is null or empty</exception>                
        public ConfigCatClient(string sdkKey, DataGovernance dataGovernance = DataGovernance.Global) : this(options => { options.SdkKey = sdkKey; options.DataGovernance = dataGovernance; })
        { }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup AutoPoll mode
        /// </summary>
        /// <param name="configuration">Configuration for AutoPolling mode</param>
        /// <exception cref="ArgumentException">When the configuration contains any invalid property</exception>
        /// <exception cref="ArgumentNullException">When the configuration is null</exception>
        [Obsolete(@"This constructor is obsolete. Please use the ConfigCatClient(options => { /* configuration options */ }) format.")]
        public ConfigCatClient(AutoPollConfiguration configuration)
            : this(options =>
            {
                if (configuration == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                configuration.ToOptions(options);
                var autoPoll = PollingModes.AutoPoll(TimeSpan.FromSeconds(configuration.PollIntervalSeconds), TimeSpan.FromSeconds(configuration.MaxInitWaitTimeSeconds));
                autoPoll.OnConfigurationChanged += configuration.RaiseOnConfigurationChanged;
                options.PollingMode = autoPoll;
            })
        { }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup LazyLoad mode
        /// </summary>
        /// <param name="configuration">Configuration for LazyLoading mode</param>
        /// <exception cref="ArgumentException">When the configuration contains any invalid property</exception>
        /// <exception cref="ArgumentNullException">When the configuration is null</exception>
        [Obsolete(@"This constructor is obsolete. Please use the ConfigCatClient(options => { /* configuration options */ }) format.")]
        public ConfigCatClient(LazyLoadConfiguration configuration)
            : this(options =>
            {
                if (configuration == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                configuration.ToOptions(options);
                options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(configuration.CacheTimeToLiveSeconds));
            })
        { }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup ManualPoll mode
        /// </summary>
        /// <param name="configuration">Configuration for LazyLoading mode</param>
        /// <exception cref="ArgumentException">When the configuration contains any invalid property</exception>
        /// <exception cref="ArgumentNullException">When the configuration is null</exception>
        [Obsolete(@"This constructor is obsolete. Please use the ConfigCatClient(options => { /* configuration options */ }) format.")]
        public ConfigCatClient(ManualPollConfiguration configuration)
            : this(options =>
            {
                if (configuration == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                configuration.ToOptions(options);
                options.PollingMode = PollingModes.ManualPoll;
            })
        { }

        /// <summary>
        /// Creates a new <see cref="ConfigCatClient"/>.
        /// </summary>
        /// <param name="configurationAction">The configuration action.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="configurationAction"/> is null.</exception>
        public ConfigCatClient(Action<ConfigCatClientOptions> configurationAction)
            : this(sdkKey: BuildConfiguration(configurationAction ?? throw new ArgumentNullException(nameof(configurationAction)), out var configuration), configuration)
        {
            this.isUncached = true;
        }

        internal ConfigCatClient(string sdkKey, ConfigCatClientOptions configuration)
        {
            this.sdkKey = sdkKey;

            this.log = new LoggerWrapper(configuration.Logger);
            this.configDeserializer = new ConfigDeserializer();
            this.configEvaluator = new RolloutEvaluator(this.log);

            var cacheParameters = new CacheParameters
            {
                ConfigCache = configuration.ConfigCache ?? new InMemoryConfigCache(),
                CacheKey = GetCacheKey(sdkKey)
            };

            if (configuration.FlagOverrides != null)
            {
                this.overrideDataSource = configuration.FlagOverrides.BuildDataSource(this.log);
                this.overrideBehaviour = configuration.FlagOverrides.OverrideBehaviour;
            }

            this.defaultUser = configuration.DefaultUser;

            this.configService = this.overrideBehaviour == null || this.overrideBehaviour != OverrideBehaviour.LocalOnly
                ? DetermineConfigService(configuration.PollingMode,
                    new HttpConfigFetcher(configuration.CreateUri(sdkKey),
                            $"{configuration.PollingMode.Identifier}-{Version}",
                            this.log,
                            configuration.HttpClientHandler,
                            this.configDeserializer,
                            configuration.IsCustomBaseUrl,
                            configuration.HttpTimeout),
                        cacheParameters,
                        this.log)
                : new EmptyConfigService();
        }

        /// <summary>
        /// For testing purposes only
        /// </summary>        
        internal ConfigCatClient(IConfigService configService, ILogger logger, IRolloutEvaluator evaluator, IConfigDeserializer configDeserializer)
        {
            this.isUncached = true;

            this.configService = configService;
            this.log = new LoggerWrapper(logger);
            this.configEvaluator = evaluator;
            this.configDeserializer = configDeserializer;
        }

        // TODO: Remove this helper when we delete the obsolete client constructors.
        private static string BuildConfiguration(Action<ConfigCatClientOptions> configurationAction, out ConfigCatClientOptions configuration)
        {
            configuration = new ConfigCatClientOptions();
            configurationAction(configuration);

            configuration.Validate();

#pragma warning disable CS0618 // Type or member is obsolete
            return configuration.SdkKey;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Returns a client object for the specified SDK Key, configured by <paramref name="configurationAction"/>.
        /// </summary>
        /// <remarks>
        /// This method returns a single, shared instance per each distinct SDK Key.
        /// That is, a new client object is created only when there is none available for the specified SDK Key.
        /// Otherwise, the already created and configured instance is returned (in which case <paramref name="configurationAction"/> is ignored).
        /// So, please keep in mind that when you make multiple calls to this method using the same SDK Key, you may end up with multiple references to the same client object.
        /// </remarks>
        /// <param name="sdkKey">SDK Key to access configuration. (For the moment, SDK Key can also be set using <paramref name="configurationAction"/>. This setting will, however, be ignored and <paramref name="sdkKey"/> will be used, regardless.)</param>
        /// <param name="configurationAction">The configuration action.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="configurationAction"/> is null.</exception>
        public static IConfigCatClient Get(string sdkKey, Action<ConfigCatClientOptions> configurationAction = null)
        {
            if (sdkKey == null)
            {
                throw new ArgumentNullException(nameof(sdkKey));
            }

            if (sdkKey.Length == 0)
            {
                throw new ArgumentException("Invalid SDK Key.", nameof(sdkKey));
            }

            // TODO: This should be simplified after SdkKey gets removed from ConfigurationBase.
            BuildConfiguration(options => 
            { 
                configurationAction?.Invoke(options);

                // For the moment, we need to set SdkKey to keep ConfigurationBase.Validate happy.
#pragma warning disable CS0618 // Type or member is obsolete
                options.SdkKey = sdkKey;
#pragma warning restore CS0618 // Type or member is obsolete
            }, out var configuration);

            var instance = Instances.GetOrCreate(sdkKey, configuration, out var instanceAlreadyCreated);

            if (instanceAlreadyCreated && configurationAction is not null)
            {
                instance.log.Warning(message: $"Client for SDK key '{sdkKey}' is already created and will be reused; configuration action is being ignored.");
            }

            return instance;
        }

        /// <inheritdoc />
        public T GetValue<T>(string key, T defaultValue, User user = null)
        {
            try
            {
                var settings = this.GetSettings();
                return this.configEvaluator.Evaluate(settings, key, defaultValue, user ?? this.defaultUser);
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
                var settings = await this.GetSettingsAsync().ConfigureAwait(false);
                return this.configEvaluator.Evaluate(settings, key, defaultValue, user ?? this.defaultUser);
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
                var settings = this.GetSettings();
                if (settings.Count == 0)
                {
                    this.log.Warning("Config deserialization failed.");
                    return ArrayUtils.EmptyArray<string>();
                }

                return settings.Keys;
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllKeys' method.\n{ex}");
                return ArrayUtils.EmptyArray<string>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAllKeysAsync()
        {
            try
            {
                var settings = await this.GetSettingsAsync().ConfigureAwait(false);
                if (settings.Count == 0)
                {
                    this.log.Warning("Config deserialization failed.");
                    return ArrayUtils.EmptyArray<string>();
                }

                return settings.Keys;
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllKeysAsync' method.\n{ex}");
                return ArrayUtils.EmptyArray<string>();
            }
        }

        /// <inheritdoc />
        public IDictionary<string, object> GetAllValues(User user = null)
        {
            try
            {
                var settings = this.GetSettings();
                return GenerateSettingKeyValueMap(user ?? this.defaultUser, settings);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllValues' method.\n{ex}");
                return new Dictionary<string, object>();
            }
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, object>> GetAllValuesAsync(User user = null)
        {
            try
            {
                var settings = await this.GetSettingsAsync().ConfigureAwait(false);
                return GenerateSettingKeyValueMap(user ?? this.defaultUser, settings);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllValuesAsync' method.\n{ex}");
                return new Dictionary<string, object>();
            }
        }


        /// <inheritdoc />
        public void ForceRefresh()
        {
            try
            {
                this.configService.RefreshConfig();
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
        ~ConfigCatClient()
        {
            // Safeguard against situations where user forgets to dispose of the client instance.

            if (!this.isUncached && this.sdkKey is not null)
            {
                Instances.Remove(this.sdkKey, out _);
            }

            Dispose(disposing: false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.configService is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                this.overrideDataSource?.Dispose();
            }
            else
            {
                // The underlying services may do some long running background work (like AutoPollConfigService or LocalFileDataSource),
                // in which case we need to signal these services to stop the background work, otherwise it would go on endlessly,
                // which would prevent the GC from collecting the service instances, that is, as an end result, would cause a memory leak 
                if (this.configService is IBackgroundWorkRunner backgroundWorkRunnerConfigService)
                {
                    backgroundWorkRunnerConfigService.Stop();
                }

                if (this.overrideDataSource is IBackgroundWorkRunner backgroundWorkRunnerOverrideDataSource)
                {
                    backgroundWorkRunnerOverrideDataSource.Stop();
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!this.isUncached)
            {
                Instances.Remove(this.sdkKey, out _);
            }

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of all existing <see cref="ConfigCatClient"/> instances.
        /// </summary>
        /// <exception cref="AggregateException">Potential exceptions thrown by <see cref="Dispose()"/> of the individual clients.</exception>
        public static void DisposeAll()
        {
            Instances.Clear(out var removedInstances);

            List<Exception> exceptions = null;
            foreach (var instance in removedInstances)
            {
                try
                {
                    instance.Dispose(disposing: true);
                    GC.SuppressFinalize(instance);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions is { Count: > 0 })
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <inheritdoc />
        public string GetVariationId(string key, string defaultVariationId, User user = null)
        {
            try
            {
                var settings = this.GetSettings();
                return this.configEvaluator.EvaluateVariationId(settings, key, defaultVariationId, user ?? this.defaultUser);
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
                var settings = await this.GetSettingsAsync().ConfigureAwait(false);
                return this.configEvaluator.EvaluateVariationId(settings, key, defaultVariationId, user ?? this.defaultUser);
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
                var settings = this.GetSettings();
                return GetAllVariationIdLogic(settings, user ?? this.defaultUser);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllVariationId' method.\n{ex}");
                return Enumerable.Empty<string>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAllVariationIdAsync(User user = null)
        {
            try
            {
                var settings = await this.GetSettingsAsync().ConfigureAwait(false);
                return GetAllVariationIdLogic(settings, user ?? this.defaultUser);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in 'GetAllVariationIdAsync' method.\n{ex}");
                return Enumerable.Empty<string>();
            }
        }

        private IEnumerable<string> GetAllVariationIdLogic(IDictionary<string, Setting> settings, User user)
        {
            if (settings.Count == 0)
            {
                this.log.Warning("Config deserialization failed.");
                return Enumerable.Empty<string>();
            }

            var result = new List<string>(settings.Keys.Count);
            result.AddRange(settings.Keys.Select(key => this.configEvaluator.EvaluateVariationId(settings, key, null, user))
                .Where(r => r != null));

            return result;
        }

        private IDictionary<string, object> GenerateSettingKeyValueMap(User user, IDictionary<string, Setting> settings)
        {
            if (settings.Count == 0)
            {
                this.log.Warning("Config deserialization failed.");
                return new Dictionary<string, object>();
            }

            return settings.ToDictionary(kv => kv.Key, kv => this.configEvaluator.Evaluate(settings, kv.Key, null, user));
        }

        /// <summary>
        /// Create a <see cref="ConfigCatClientBuilder"/> instance to setup the client
        /// </summary>
        /// <param name="sdkKey"></param>
        /// <returns></returns>
        [Obsolete("Please use the 'new ConfigCatClient(options => { /* configuration options */ })' format to instantiate a new ConfigCatClient.")]
        public static ConfigCatClientBuilder Create(string sdkKey)
        {
            return ConfigCatClientBuilder.Initialize(sdkKey);
        }

        private IDictionary<string, Setting> GetSettings()
        {
            if (this.overrideBehaviour != null)
            {
                IDictionary<string, Setting> local;
                IDictionary<string, Setting> remote;
                switch (this.overrideBehaviour)
                {
                    case OverrideBehaviour.LocalOnly:
                        return this.overrideDataSource.GetOverrides();
                    case OverrideBehaviour.LocalOverRemote:
                        local = this.overrideDataSource.GetOverrides();
                        remote = GetRemoteConfig();
                        return remote.MergeOverwriteWith(local);
                    case OverrideBehaviour.RemoteOverLocal:
                        local = this.overrideDataSource.GetOverrides();
                        remote = GetRemoteConfig();
                        return local.MergeOverwriteWith(remote);
                }
            }

            return GetRemoteConfig();

            IDictionary<string, Setting> GetRemoteConfig()
            {
                var config = this.configService.GetConfig();
                if (!this.configDeserializer.TryDeserialize(config.JsonString, out var deserialized))
                    return new Dictionary<string, Setting>();

                return deserialized.Settings;
            }
        }

        private async Task<IDictionary<string, Setting>> GetSettingsAsync()
        {
            if (this.overrideBehaviour != null)
            {
                IDictionary<string, Setting> local;
                IDictionary<string, Setting> remote;
                switch (this.overrideBehaviour)
                {
                    case OverrideBehaviour.LocalOnly:
                        return await this.overrideDataSource.GetOverridesAsync().ConfigureAwait(false);
                    case OverrideBehaviour.LocalOverRemote:
                        local = await this.overrideDataSource.GetOverridesAsync().ConfigureAwait(false);
                        remote = await GetRemoteConfigAsync().ConfigureAwait(false);
                        return remote.MergeOverwriteWith(local);
                    case OverrideBehaviour.RemoteOverLocal:
                        local = await this.overrideDataSource.GetOverridesAsync().ConfigureAwait(false);
                        remote = await GetRemoteConfigAsync().ConfigureAwait(false);
                        return local.MergeOverwriteWith(remote);
                }
            }

            return await GetRemoteConfigAsync().ConfigureAwait(false);

            async Task<IDictionary<string, Setting>> GetRemoteConfigAsync()
            {
                var config = await this.configService.GetConfigAsync().ConfigureAwait(false);
                if (!this.configDeserializer.TryDeserialize(config.JsonString, out var deserialized))
                    return new Dictionary<string, Setting>();

                return deserialized.Settings;
            }
        }

        private static IConfigService DetermineConfigService(PollingMode pollingMode, HttpConfigFetcher fetcher, CacheParameters cacheParameters, LoggerWrapper logger)
        {
            return pollingMode switch
            {
                AutoPoll autoPoll => new AutoPollConfigService(autoPoll,
                    fetcher,
                    cacheParameters,
                    logger),
                LazyLoad lazyLoad => new LazyLoadConfigService(fetcher,
                    cacheParameters,
                    logger,
                    lazyLoad.CacheTimeToLive),
                ManualPoll => new ManualPollConfigService(fetcher,
                    cacheParameters,
                    logger),
                _ => throw new ArgumentException("Invalid configuration type."),
            };
        }

        private static string GetCacheKey(string sdkKey)
        {
            var key = $"dotnet_{ConfigurationBase.ConfigFileName}_{sdkKey}";
            return key.Hash();
        }

        /// <inheritdoc />
        public void SetDefaultUser(User user)
        {
            this.defaultUser = user ?? throw new ArgumentNullException(nameof(user));
        }

        /// <inheritdoc />
        public void ClearDefaultUser()
        {
            this.defaultUser = null;
        }
    }
}