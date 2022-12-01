using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Override;
using System.Threading;

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
        private readonly Hooks hooks;
        // NOTE: The following mutable field(s) may be accessed from multiple threads and we need to make sure that changes to them are observable in these threads.
        // Volatile guarantees (see https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile) that such changes become visible to them *eventually*,
        // which is good enough in these cases.
        private volatile User defaultUser;

        private static readonly string Version = typeof(ConfigCatClient).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        internal static readonly ConfigCatClientCache Instances = new();

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
        [Obsolete("This constructor is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
        public ConfigCatClient(string sdkKey, DataGovernance dataGovernance = DataGovernance.Global) : this(options => { options.SdkKey = sdkKey; options.DataGovernance = dataGovernance; })
        { }

        /// <summary>
        /// Create an instance of ConfigCatClient and setup AutoPoll mode
        /// </summary>
        /// <param name="configuration">Configuration for AutoPolling mode</param>
        /// <exception cref="ArgumentException">When the configuration contains any invalid property</exception>
        /// <exception cref="ArgumentNullException">When the configuration is null</exception>
        [Obsolete("This constructor is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
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
        [Obsolete("This constructor is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
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
        [Obsolete("This constructor is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
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
        [Obsolete("This constructor is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
        public ConfigCatClient(Action<ConfigCatClientOptions> configurationAction)
            : this(sdkKey: BuildConfiguration(configurationAction ?? throw new ArgumentNullException(nameof(configurationAction)), out var configuration), configuration)
        {
            this.isUncached = true;
        }

        internal ConfigCatClient(string sdkKey, ConfigCatClientOptions configuration)
        {
            this.sdkKey = sdkKey;
            this.hooks = configuration.Hooks;
            this.hooks.SetSender(this);

            this.log = new LoggerWrapper(configuration.Logger, this.hooks);
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
                        this.log,
                        configuration.Offline,
                        this.hooks)
                : new NullConfigService(this.log, this.hooks);
        }

        /// <summary>
        /// For testing purposes only
        /// </summary>        
        internal ConfigCatClient(IConfigService configService, ILogger logger, IRolloutEvaluator evaluator, IConfigDeserializer configDeserializer, Hooks hooks = null)
        {
            this.isUncached = true;

            if (hooks is not null)
            {
                this.hooks = hooks;
                this.hooks.SetSender(this);
            }
            else
            {
                this.hooks = NullHooks.Instance;
            }

            this.configService = configService;
            this.log = new LoggerWrapper(logger, this.hooks);
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
            T value;
            EvaluationDetails<T> evaluationDetails;
            SettingsWithRemoteConfig settings = default;
            user ??= this.defaultUser;
            try
            {
                typeof(T).EnsureSupportedSettingClrType();
                settings = this.GetSettings();
                value = this.configEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, this.log, out evaluationDetails);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetValue)}' method.", ex);
                evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
                value = defaultValue;
            }

            this.hooks.RaiseFlagEvaluated(evaluationDetails);
            return value;
        }

        /// <inheritdoc />
        public async Task<T> GetValueAsync<T>(string key, T defaultValue, User user = null)
        {
            T value;
            EvaluationDetails<T> evaluationDetails;
            SettingsWithRemoteConfig settings = default;
            user ??= this.defaultUser;
            try
            {
                typeof(T).EnsureSupportedSettingClrType();
                settings = await this.GetSettingsAsync().ConfigureAwait(false);
                value = this.configEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, this.log, out evaluationDetails);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetValueAsync)}' method.", ex);
                evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
                value = defaultValue;
            }

            this.hooks.RaiseFlagEvaluated(evaluationDetails);
            return value;
        }

        /// <inheritdoc />
        public EvaluationDetails<T> GetValueDetails<T>(string key, T defaultValue, User user = null)
        {
            EvaluationDetails<T> evaluationDetails;
            SettingsWithRemoteConfig settings = default;
            user ??= this.defaultUser;
            try
            {
                typeof(T).EnsureSupportedSettingClrType();
                settings = this.GetSettings();
                this.configEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, this.log, out evaluationDetails);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetValueDetails)}' method.", ex);
                evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
            }

            this.hooks.RaiseFlagEvaluated(evaluationDetails);
            return evaluationDetails;
        }

        /// <inheritdoc />
        public async Task<EvaluationDetails<T>> GetValueDetailsAsync<T>(string key, T defaultValue, User user = null)
        {
            EvaluationDetails<T> evaluationDetails;
            SettingsWithRemoteConfig settings = default;
            user ??= this.defaultUser;
            try
            {
                typeof(T).EnsureSupportedSettingClrType();
                settings = await this.GetSettingsAsync().ConfigureAwait(false);
                this.configEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, this.log, out evaluationDetails);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetValueDetailsAsync)}' method.", ex);
                evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
            }

            this.hooks.RaiseFlagEvaluated(evaluationDetails);
            return evaluationDetails;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            try
            {
                var settings = this.GetSettings();
                if (!RolloutEvaluatorExtensions.CheckSettingsAvailable(settings.Value, this.log))
                {
                    return Enumerable.Empty<string>();
                }
                return settings.Value.Keys;
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetAllKeys)}' method.", ex);
                return Enumerable.Empty<string>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAllKeysAsync()
        {
            try
            {
                var settings = await this.GetSettingsAsync().ConfigureAwait(false);
                if (!RolloutEvaluatorExtensions.CheckSettingsAvailable(settings.Value, this.log))
                {
                    return Enumerable.Empty<string>();
                }
                return settings.Value.Keys;
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetAllKeysAsync)}' method.", ex);
                return Enumerable.Empty<string>();
            }
        }

        /// <inheritdoc />
        public IDictionary<string, object> GetAllValues(User user = null)
        {
            IDictionary<string, object> result;
            EvaluationDetails[] evaluationDetailsArray = null;
            user ??= this.defaultUser;
            try
            {
                var settings = this.GetSettings();
                result = this.configEvaluator.EvaluateAll(settings.Value, user, settings.RemoteConfig, this.log, out evaluationDetailsArray);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetAllValues)}' method.", ex);
                return new Dictionary<string, object>();
            }

            foreach (var evaluationDetails in evaluationDetailsArray)
            {
                this.hooks.RaiseFlagEvaluated(evaluationDetails);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, object>> GetAllValuesAsync(User user = null)
        {
            IDictionary<string, object> result;
            EvaluationDetails[] evaluationDetailsArray = null;
            user ??= this.defaultUser;
            try
            {
                var settings = await this.GetSettingsAsync().ConfigureAwait(false);
                result = this.configEvaluator.EvaluateAll(settings.Value, user, settings.RemoteConfig, this.log, out evaluationDetailsArray);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetAllValuesAsync)}' method.", ex);
                return new Dictionary<string, object>();
            }

            foreach (var evaluationDetails in evaluationDetailsArray)
            {
                this.hooks.RaiseFlagEvaluated(evaluationDetails);
            }

            return result;
        }

        /// <inheritdoc />
        public RefreshResult ForceRefresh()
        {
            try
            {
                return this.configService.RefreshConfig();
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(ForceRefresh)}' method.", ex);
                return RefreshResult.Failure(ex.Message, ex);
            }
        }

        /// <inheritdoc />
        public async Task<RefreshResult> ForceRefreshAsync()
        {
            try
            {
                return await this.configService.RefreshConfigAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(ForceRefreshAsync)}' method.", ex);
                return RefreshResult.Failure(ex.Message, ex);
            }
        }

        /// <inheritdoc />
        ~ConfigCatClient()
        {
            // Safeguard against situations where user forgets to dispose of the client instance.

            if (!this.isUncached && this.sdkKey is not null)
            {
                Instances.Remove(this.sdkKey, instanceToRemove: this);
            }

            Dispose(disposing: false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (this.hooks.TryDisconnect(out var raiseBeforeClientDispose))
                    {
                        raiseBeforeClientDispose?.Invoke(this);
                    }
                }
                finally
                {
                    if (this.configService is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    this.overrideDataSource?.Dispose();
                }
            }
            else
            {
                try
                {
                    // NOTE: hooks may be null (e.g. if already collected) when this method is called by the finalizer
                    this.hooks?.TryDisconnect(out _);
                }
                finally
                {
                    // Execution gets here when consumer forgets to dispose the client instance.
                    // In this case we need to make sure that background work is stopped,
                    // otherwise it would go on endlessly, that is, we'd end up with a memory leak.
                    var autoPollConfigService = this.configService as AutoPollConfigService;
                    var localFileDataSource = this.overrideDataSource as LocalFileDataSource;
                    if (autoPollConfigService is not null || localFileDataSource is not null)
                    {
                        Task.Run(() =>
                        {
                            autoPollConfigService?.StopScheduler();
                            localFileDataSource?.StopWatch();
                        });
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!this.isUncached)
            {
                Instances.Remove(this.sdkKey, instanceToRemove: this);
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
            string variationId;
            EvaluationDetails evaluationDetails;
            SettingsWithRemoteConfig settings = default;
            user ??= this.defaultUser;
            try
            {
                settings = this.GetSettings();
                variationId = this.configEvaluator.EvaluateVariationId(settings.Value, key, defaultVariationId, user, settings.RemoteConfig, this.log, out evaluationDetails);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetVariationId)}' method.", ex);
                evaluationDetails = EvaluationDetails.FromDefaultVariationId(key, defaultVariationId, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
                variationId = defaultVariationId;
            }

            this.hooks.RaiseFlagEvaluated(evaluationDetails);
            return variationId;
        }

        /// <inheritdoc />
        public async Task<string> GetVariationIdAsync(string key, string defaultVariationId, User user = null)
        {
            string variationId;
            EvaluationDetails evaluationDetails;
            SettingsWithRemoteConfig settings = default;
            user ??= this.defaultUser;
            try
            {
                settings = await this.GetSettingsAsync().ConfigureAwait(false);
                variationId = this.configEvaluator.EvaluateVariationId(settings.Value, key, defaultVariationId, user, settings.RemoteConfig, this.log, out evaluationDetails);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetVariationIdAsync)}' method.", ex);
                evaluationDetails = EvaluationDetails.FromDefaultVariationId(key, defaultVariationId, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
                variationId = defaultVariationId;
            }

            this.hooks.RaiseFlagEvaluated(evaluationDetails);
            return variationId;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllVariationId(User user = null)
        {
            IList<string> result;
            EvaluationDetails[] evaluationDetailsArray = null;
            user ??= this.defaultUser;
            try
            {
                var settings = this.GetSettings();
                result = this.configEvaluator.EvaluateAllVariationIds(settings.Value, user, settings.RemoteConfig, this.log, out evaluationDetailsArray);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetAllVariationId)}' method.", ex);
                return Enumerable.Empty<string>();
            }

            foreach (var evaluationDetails in evaluationDetailsArray)
            {
                this.hooks.RaiseFlagEvaluated(evaluationDetails);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAllVariationIdAsync(User user = null)
        {
            IList<string> result;
            EvaluationDetails[] evaluationDetailsArray = null;
            user ??= this.defaultUser;
            try
            {
                var settings = await this.GetSettingsAsync().ConfigureAwait(false);
                result = this.configEvaluator.EvaluateAllVariationIds(settings.Value, user, settings.RemoteConfig, this.log, out evaluationDetailsArray);
            }
            catch (Exception ex)
            {
                this.log.Error($"Error occured in '{nameof(GetAllVariationIdAsync)}' method.", ex);
                return Enumerable.Empty<string>();
            }

            foreach (var evaluationDetails in evaluationDetailsArray)
            {
                this.hooks.RaiseFlagEvaluated(evaluationDetails);
            }

            return result;
        }

        /// <summary>
        /// Create a <see cref="ConfigCatClientBuilder"/> instance to setup the client
        /// </summary>
        /// <param name="sdkKey"></param>
        /// <returns></returns>
        [Obsolete("To obtain a ConfigCatClient instance for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { /* configuration options */ })' format.")]
        public static ConfigCatClientBuilder Create(string sdkKey)
        {
            return ConfigCatClientBuilder.Initialize(sdkKey);
        }

        private SettingsWithRemoteConfig GetSettings()
        {
            if (this.overrideBehaviour != null)
            {
                IDictionary<string, Setting> local;
                SettingsWithRemoteConfig remote;
                switch (this.overrideBehaviour)
                {
                    case OverrideBehaviour.LocalOnly:
                        return new SettingsWithRemoteConfig(this.overrideDataSource.GetOverrides(), remoteConfig: null);
                    case OverrideBehaviour.LocalOverRemote:
                        local = this.overrideDataSource.GetOverrides();
                        remote = GetRemoteConfig();
                        return new SettingsWithRemoteConfig(remote.Value.MergeOverwriteWith(local), remote.RemoteConfig);
                    case OverrideBehaviour.RemoteOverLocal:
                        local = this.overrideDataSource.GetOverrides();
                        remote = GetRemoteConfig();
                        return new SettingsWithRemoteConfig(local.MergeOverwriteWith(remote.Value), remote.RemoteConfig);
                }
            }

            return GetRemoteConfig();

            SettingsWithRemoteConfig GetRemoteConfig()
            {
                var config = this.configService.GetConfig();
                if (!this.configDeserializer.TryDeserialize(config.JsonString, config.HttpETag, out var deserialized))
                    return new SettingsWithRemoteConfig(new Dictionary<string, Setting>(), config);

                return new SettingsWithRemoteConfig(deserialized.Settings, config);
            }
        }

        private async Task<SettingsWithRemoteConfig> GetSettingsAsync()
        {
            if (this.overrideBehaviour != null)
            {
                IDictionary<string, Setting> local;
                SettingsWithRemoteConfig remote;
                switch (this.overrideBehaviour)
                {
                    case OverrideBehaviour.LocalOnly:
                        return new SettingsWithRemoteConfig(await this.overrideDataSource.GetOverridesAsync().ConfigureAwait(false), remoteConfig: null);
                    case OverrideBehaviour.LocalOverRemote:
                        local = await this.overrideDataSource.GetOverridesAsync().ConfigureAwait(false);
                        remote = await GetRemoteConfigAsync().ConfigureAwait(false);
                        return new SettingsWithRemoteConfig(remote.Value.MergeOverwriteWith(local), remote.RemoteConfig);
                    case OverrideBehaviour.RemoteOverLocal:
                        local = await this.overrideDataSource.GetOverridesAsync().ConfigureAwait(false);
                        remote = await GetRemoteConfigAsync().ConfigureAwait(false);
                        return new SettingsWithRemoteConfig(local.MergeOverwriteWith(remote.Value), remote.RemoteConfig);
                }
            }

            return await GetRemoteConfigAsync().ConfigureAwait(false);

            async Task<SettingsWithRemoteConfig> GetRemoteConfigAsync()
            {
                var config = await this.configService.GetConfigAsync().ConfigureAwait(false);
                if (!this.configDeserializer.TryDeserialize(config.JsonString, config.HttpETag, out var deserialized))
                    return new SettingsWithRemoteConfig(new Dictionary<string, Setting>(), config);

                return new SettingsWithRemoteConfig(deserialized.Settings, config);
            }
        }

        private static IConfigService DetermineConfigService(PollingMode pollingMode, HttpConfigFetcher fetcher, CacheParameters cacheParameters, LoggerWrapper logger, bool isOffline, Hooks hooks)
        {
            return pollingMode switch
            {
                AutoPoll autoPoll => new AutoPollConfigService(autoPoll,
                    fetcher,
                    cacheParameters,
                    logger,
                    isOffline,
                    hooks),
                LazyLoad lazyLoad => new LazyLoadConfigService(fetcher,
                    cacheParameters,
                    logger,
                    lazyLoad.CacheTimeToLive,
                    isOffline,
                    hooks),
                ManualPoll => new ManualPollConfigService(fetcher,
                    cacheParameters,
                    logger,
                    isOffline,
                    hooks),
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

        /// <inheritdoc />
        public bool IsOffline => this.configService.IsOffline;

        /// <inheritdoc />
        public void SetOnline()
        {
            this.configService.SetOnline();
        }

        /// <inheritdoc />
        public void SetOffline()
        {
            this.configService.SetOffline();
        }

        /// <inheritdoc/>
        public event EventHandler ClientReady
        {
            add { this.hooks.ClientReady += value; }
            remove { this.hooks.ClientReady -= value; }
        }

        /// <inheritdoc/>
        public event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated
        {
            add { this.hooks.FlagEvaluated += value; }
            remove { this.hooks.FlagEvaluated -= value; }
        }

        /// <inheritdoc/>
        public event EventHandler<ConfigChangedEventArgs> ConfigChanged
        {
            add { this.hooks.ConfigChanged += value; }
            remove { this.hooks.ConfigChanged -= value; }
        }

        /// <inheritdoc/>
        public event EventHandler<ConfigCatClientErrorEventArgs> Error
        {
            add { this.hooks.Error += value; }
            remove { this.hooks.Error -= value; }
        }

        /// <inheritdoc/>
        public event EventHandler BeforeClientDispose
        {
            add { this.hooks.BeforeClientDispose += value; }
            remove { this.hooks.BeforeClientDispose -= value; }
        }

        private readonly struct SettingsWithRemoteConfig
        {
            public SettingsWithRemoteConfig(IDictionary<string, Setting> value, ProjectConfig remoteConfig)
            {
                Value = value;
                RemoteConfig = remoteConfig;
            }

            public IDictionary<string, Setting> Value { get; }
            public ProjectConfig RemoteConfig { get; }
        }
    }
}