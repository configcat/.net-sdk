using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Override;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// ConfigCat SDK client.
/// </summary>
public sealed class ConfigCatClient : IConfigCatClient
{
    private static readonly string Version = typeof(ConfigCatClient).GetTypeInfo().Assembly.GetName().Version!.ToString(fieldCount: 3);

    internal static readonly ConfigCatClientCache Instances = new();

    private readonly string? sdkKey; // may be null in case of testing
    private readonly LoggerWrapper logger;
    private readonly IRolloutEvaluator configEvaluator;
    private readonly IConfigService configService;
    private readonly IOverrideDataSource? overrideDataSource;
    private readonly OverrideBehaviour? overrideBehaviour;
    private readonly Hooks hooks;
    // NOTE: The following mutable field(s) may be accessed from multiple threads and we need to make sure that changes to them are observable in these threads.
    // Volatile guarantees (see https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile) that such changes become visible to them *eventually*,
    // which is good enough in these cases.
    private volatile User? defaultUser;

    private static bool IsValidSdkKey(string sdkKey, bool customBaseUrl)
    {
        const string proxyPrefix = "configcat-proxy/";

        if (customBaseUrl && sdkKey.Length > proxyPrefix.Length && sdkKey.StartsWith(proxyPrefix, StringComparison.Ordinal))
        {
            return true;
        }

        var components = sdkKey.Split('/');
        const int keyLength = 22;

        return components.Length switch
        {
            2 => components[0].Length == keyLength && components[1].Length == keyLength,
            3 => components[0] == "configcat-sdk-1" && components[1].Length == keyLength && components[2].Length == keyLength,
            _ => false
        };
    }

    internal static string GetProductVersion(PollingMode pollingMode)
    {
        return $"{pollingMode.Identifier}-{Version}";
    }

    internal static string GetCacheKey(string sdkKey)
    {
        var key = $"{sdkKey}_{ConfigCatClientOptions.ConfigFileName}_{ProjectConfig.SerializationFormatVersion}";
        return key.Sha1().ToHexString();
    }

    /// <inheritdoc />
    public LogLevel LogLevel
    {
        get => this.logger.LogLevel;
        set => this.logger.LogLevel = value;
    }

    internal ConfigCatClient(string sdkKey, ConfigCatClientOptions options)
    {
        this.sdkKey = sdkKey;
        this.hooks = options.Hooks;
        this.hooks.SetSender(this);

        this.logger = new LoggerWrapper(options.Logger ?? ConfigCatClientOptions.CreateDefaultLogger(), this.hooks);
        this.configEvaluator = new RolloutEvaluator(this.logger);

        var cacheParameters = new CacheParameters
        (
            configCache: options.ConfigCache is not null
                ? new ExternalConfigCache(options.ConfigCache, this.logger)
                : ConfigCatClientOptions.CreateDefaultConfigCache(),
            cacheKey: GetCacheKey(sdkKey)
        );

        if (options.FlagOverrides is not null)
        {
            this.overrideDataSource = options.FlagOverrides.BuildDataSource(this.logger);
            this.overrideBehaviour = options.FlagOverrides.OverrideBehaviour;
        }

        this.defaultUser = options.DefaultUser;

        var pollingMode = options.PollingMode ?? ConfigCatClientOptions.CreateDefaultPollingMode();

        this.configService = this.overrideBehaviour != OverrideBehaviour.LocalOnly
            ? DetermineConfigService(pollingMode,
                new HttpConfigFetcher(options.CreateUri(sdkKey),
                        GetProductVersion(pollingMode),
                        this.logger,
                        options.HttpClientHandler,
                        options.IsCustomBaseUrl,
                        options.HttpTimeout),
                    cacheParameters,
                    this.logger,
                    options.Offline,
                    this.hooks)
            : new NullConfigService(this.logger, this.hooks);
    }

    // For test purposes only
    internal ConfigCatClient(IConfigService configService, IConfigCatLogger logger, IRolloutEvaluator evaluator, Hooks? hooks = null)
    {
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
        this.logger = new LoggerWrapper(logger, this.hooks);
        this.configEvaluator = evaluator;
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
    /// <param name="sdkKey">SDK Key to access the ConfigCat config.</param>
    /// <param name="configurationAction">The action used to configure the client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sdkKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sdkKey"/> is an empty string or in an invalid format.</exception>
    public static IConfigCatClient Get(string sdkKey, Action<ConfigCatClientOptions>? configurationAction = null)
    {
        if (sdkKey is null)
        {
            throw new ArgumentNullException(nameof(sdkKey));
        }

        if (sdkKey.Length == 0)
        {
            throw new ArgumentException("SDK Key cannot be empty.", nameof(sdkKey));
        }

        var options = new ConfigCatClientOptions();
        configurationAction?.Invoke(options);

        if (options.FlagOverrides is not { OverrideBehaviour: OverrideBehaviour.LocalOnly } && !IsValidSdkKey(sdkKey, options.IsCustomBaseUrl))
        {
            throw new ArgumentException($"SDK Key '{sdkKey}' is invalid.", nameof(sdkKey));
        }

        var instance = Instances.GetOrCreate(sdkKey, options, out var instanceAlreadyCreated);

        if (instanceAlreadyCreated && configurationAction is not null)
        {
            instance.logger.ClientIsAlreadyCreated(sdkKey);
        }

        return instance;
    }

    /// <inheritdoc />
    ~ConfigCatClient()
    {
        // Safeguard against situations where user forgets to dispose the client instance.

        if (this.sdkKey is not null)
        {
            Instances.Remove(this.sdkKey, instanceToRemove: this);
        }

        Dispose(disposing: false);
    }

    private void Dispose(bool disposing)
    {
        // NOTE: hooks may be null (e.g. if already collected) when this method is called by the finalizer
        this.hooks?.TryDisconnect();

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

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.sdkKey is not null)
        {
            Instances.Remove(this.sdkKey, instanceToRemove: this);
        }

        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes all existing <see cref="ConfigCatClient"/> instances.
    /// </summary>
    /// <exception cref="AggregateException">Potential exceptions thrown by <see cref="Dispose()"/> of the individual clients.</exception>
    public static void DisposeAll()
    {
        Instances.Clear(out var removedInstances);

        List<Exception>? exceptions = null;
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
    public T GetValue<T>(string key, T defaultValue, User? user = null)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key.Length == 0)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

        typeof(T).EnsureSupportedSettingClrType(nameof(T));

        T value;
        EvaluationDetails<T> evaluationDetails;
        SettingsWithRemoteConfig settings = default;
        user ??= this.defaultUser;
        try
        {
            settings = GetSettings();
            evaluationDetails = this.configEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, this.logger);
            value = evaluationDetails.Value;
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetValue), key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
            value = defaultValue;
        }

        this.hooks.RaiseFlagEvaluated(evaluationDetails);
        return value;
    }

    /// <inheritdoc />
    public async Task<T> GetValueAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key.Length == 0)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

        typeof(T).EnsureSupportedSettingClrType(nameof(T));

        T value;
        EvaluationDetails<T> evaluationDetails;
        SettingsWithRemoteConfig settings = default;
        user ??= this.defaultUser;
        try
        {
            settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
            evaluationDetails = this.configEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, this.logger);
            value = evaluationDetails.Value;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetValueAsync), key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
            value = defaultValue;
        }

        this.hooks.RaiseFlagEvaluated(evaluationDetails);
        return value;
    }

    /// <inheritdoc />
    public EvaluationDetails<T> GetValueDetails<T>(string key, T defaultValue, User? user = null)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key.Length == 0)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

        typeof(T).EnsureSupportedSettingClrType(nameof(T));

        EvaluationDetails<T> evaluationDetails;
        SettingsWithRemoteConfig settings = default;
        user ??= this.defaultUser;
        try
        {
            settings = GetSettings();
            evaluationDetails = this.configEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, this.logger);
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetValueDetails), key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
        }

        this.hooks.RaiseFlagEvaluated(evaluationDetails);
        return evaluationDetails;
    }

    /// <inheritdoc />
    public async Task<EvaluationDetails<T>> GetValueDetailsAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key.Length == 0)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

        typeof(T).EnsureSupportedSettingClrType(nameof(T));

        EvaluationDetails<T> evaluationDetails;
        SettingsWithRemoteConfig settings = default;
        user ??= this.defaultUser;
        try
        {
            settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
            evaluationDetails = this.configEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, this.logger);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetValueDetailsAsync), key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
        }

        this.hooks.RaiseFlagEvaluated(evaluationDetails);
        return evaluationDetails;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetAllKeys()
    {
        const string defaultReturnValue = "empty collection";
        try
        {
            var settings = GetSettings();
            if (!RolloutEvaluatorExtensions.CheckSettingsAvailable(settings.Value, this.logger, defaultReturnValue))
            {
                return ArrayUtils.EmptyArray<string>();
            }
            return settings.Value.ReadOnlyKeys();
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetAllKeys), defaultReturnValue, ex);
            return ArrayUtils.EmptyArray<string>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetAllKeysAsync(CancellationToken cancellationToken = default)
    {
        const string defaultReturnValue = "empty collection";
        try
        {
            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
            if (!RolloutEvaluatorExtensions.CheckSettingsAvailable(settings.Value, this.logger, defaultReturnValue))
            {
                return ArrayUtils.EmptyArray<string>();
            }
            return settings.Value.ReadOnlyKeys();
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetAllKeysAsync), defaultReturnValue, ex);
            return ArrayUtils.EmptyArray<string>();
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> GetAllValues(User? user = null)
    {
        const string defaultReturnValue = "empty dictionary";
        Dictionary<string, object?> result;
        EvaluationDetails[]? evaluationDetailsArray;
        IReadOnlyList<Exception>? evaluationExceptions;
        user ??= this.defaultUser;
        try
        {
            var settings = GetSettings();
            evaluationDetailsArray = this.configEvaluator.EvaluateAll(settings.Value, user, settings.RemoteConfig, this.logger, defaultReturnValue, out evaluationExceptions);
            result = evaluationDetailsArray.ToDictionary(details => details.Key, details => details.Value);
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetAllValues), defaultReturnValue, ex);
            return new Dictionary<string, object?>();
        }

        if (evaluationExceptions is { Count: > 0 })
        {
            this.logger.SettingEvaluationError(nameof(GetAllValues), "evaluation result", new AggregateException(evaluationExceptions));
        }

        foreach (var evaluationDetails in evaluationDetailsArray)
        {
            this.hooks.RaiseFlagEvaluated(evaluationDetails);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>> GetAllValuesAsync(User? user = null, CancellationToken cancellationToken = default)
    {
        const string defaultReturnValue = "empty dictionary";
        Dictionary<string, object?> result;
        EvaluationDetails[]? evaluationDetailsArray;
        IReadOnlyList<Exception>? evaluationExceptions;
        user ??= this.defaultUser;
        try
        {
            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
            evaluationDetailsArray = this.configEvaluator.EvaluateAll(settings.Value, user, settings.RemoteConfig, this.logger, defaultReturnValue, out evaluationExceptions);
            result = evaluationDetailsArray.ToDictionary(details => details.Key, details => details.Value);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetAllValuesAsync), defaultReturnValue, ex);
            return new Dictionary<string, object?>();
        }

        if (evaluationExceptions is { Count: > 0 })
        {
            this.logger.SettingEvaluationError(nameof(GetAllValuesAsync), "evaluation result", new AggregateException(evaluationExceptions));
        }

        foreach (var evaluationDetails in evaluationDetailsArray)
        {
            this.hooks.RaiseFlagEvaluated(evaluationDetails);
        }

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<EvaluationDetails> GetAllValueDetails(User? user = null)
    {
        const string defaultReturnValue = "empty list";
        EvaluationDetails[]? evaluationDetailsArray;
        IReadOnlyList<Exception>? evaluationExceptions;
        user ??= this.defaultUser;
        try
        {
            var settings = GetSettings();
            evaluationDetailsArray = this.configEvaluator.EvaluateAll(settings.Value, user, settings.RemoteConfig, this.logger, defaultReturnValue, out evaluationExceptions);
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetAllValueDetails), defaultReturnValue, ex);
            return ArrayUtils.EmptyArray<EvaluationDetails>();
        }

        if (evaluationExceptions is { Count: > 0 })
        {
            this.logger.SettingEvaluationError(nameof(GetAllValueDetails), "evaluation result", new AggregateException(evaluationExceptions));
        }

        foreach (var evaluationDetails in evaluationDetailsArray)
        {
            this.hooks.RaiseFlagEvaluated(evaluationDetails);
        }

        return evaluationDetailsArray;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EvaluationDetails>> GetAllValueDetailsAsync(User? user = null, CancellationToken cancellationToken = default)
    {
        const string defaultReturnValue = "empty list";
        EvaluationDetails[]? evaluationDetailsArray;
        IReadOnlyList<Exception>? evaluationExceptions;
        user ??= this.defaultUser;
        try
        {
            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
            evaluationDetailsArray = this.configEvaluator.EvaluateAll(settings.Value, user, settings.RemoteConfig, this.logger, defaultReturnValue, out evaluationExceptions);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.SettingEvaluationError(nameof(GetAllValueDetailsAsync), defaultReturnValue, ex);
            return ArrayUtils.EmptyArray<EvaluationDetails>();
        }

        if (evaluationExceptions is { Count: > 0 })
        {
            this.logger.SettingEvaluationError(nameof(GetAllValueDetailsAsync), "evaluation result", new AggregateException(evaluationExceptions));
        }

        foreach (var evaluationDetails in evaluationDetailsArray)
        {
            this.hooks.RaiseFlagEvaluated(evaluationDetails);
        }

        return evaluationDetailsArray;
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
            this.logger.ForceRefreshError(nameof(ForceRefresh), ex);
            return RefreshResult.Failure(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task<RefreshResult> ForceRefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await this.configService.RefreshConfigAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.ForceRefreshError(nameof(ForceRefreshAsync), ex);
            return RefreshResult.Failure(ex.Message, ex);
        }
    }

    private SettingsWithRemoteConfig GetSettings()
    {
        Dictionary<string, Setting> local;
        SettingsWithRemoteConfig remote;
        switch (this.overrideBehaviour)
        {
            case null:
                return GetRemoteConfig();
            case OverrideBehaviour.LocalOnly:
                return new SettingsWithRemoteConfig(this.overrideDataSource!.GetOverrides(), remoteConfig: null);
            case OverrideBehaviour.LocalOverRemote:
                local = this.overrideDataSource!.GetOverrides();
                remote = GetRemoteConfig();
                return new SettingsWithRemoteConfig(remote.Value.MergeOverwriteWith(local), remote.RemoteConfig);
            case OverrideBehaviour.RemoteOverLocal:
                local = this.overrideDataSource!.GetOverrides();
                remote = GetRemoteConfig();
                return new SettingsWithRemoteConfig(local.MergeOverwriteWith(remote.Value), remote.RemoteConfig);
            default:
                throw new InvalidOperationException(); // execution should never get here
        }

        SettingsWithRemoteConfig GetRemoteConfig()
        {
            var config = this.configService.GetConfig();
            var settings = !config.IsEmpty ? config.Config.Settings : null;
            return new SettingsWithRemoteConfig(settings, config);
        }
    }

    private async Task<SettingsWithRemoteConfig> GetSettingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, Setting> local;
        SettingsWithRemoteConfig remote;
        switch (this.overrideBehaviour)
        {
            case null:
                return await GetRemoteConfigAsync(cancellationToken).ConfigureAwait(false);
            case OverrideBehaviour.LocalOnly:
                return new SettingsWithRemoteConfig(await this.overrideDataSource!.GetOverridesAsync(cancellationToken).ConfigureAwait(false), remoteConfig: null);
            case OverrideBehaviour.LocalOverRemote:
                local = await this.overrideDataSource!.GetOverridesAsync(cancellationToken).ConfigureAwait(false);
                remote = await GetRemoteConfigAsync(cancellationToken).ConfigureAwait(false);
                return new SettingsWithRemoteConfig(remote.Value.MergeOverwriteWith(local), remote.RemoteConfig);
            case OverrideBehaviour.RemoteOverLocal:
                local = await this.overrideDataSource!.GetOverridesAsync(cancellationToken).ConfigureAwait(false);
                remote = await GetRemoteConfigAsync(cancellationToken).ConfigureAwait(false);
                return new SettingsWithRemoteConfig(local.MergeOverwriteWith(remote.Value), remote.RemoteConfig);
            default:
                throw new InvalidOperationException(); // execution should never get here
        }

        async Task<SettingsWithRemoteConfig> GetRemoteConfigAsync(CancellationToken cancellationToken)
        {
            var config = await this.configService.GetConfigAsync(cancellationToken).ConfigureAwait(false);
            var settings = !config.IsEmpty ? config.Config.Settings : null;
            return new SettingsWithRemoteConfig(settings, config);
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
            _ => throw new ArgumentException("Invalid polling mode.", nameof(pollingMode)),
        };
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
    public event EventHandler? ClientReady
    {
        add { this.hooks.ClientReady += value; }
        remove { this.hooks.ClientReady -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated
    {
        add { this.hooks.FlagEvaluated += value; }
        remove { this.hooks.FlagEvaluated -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged
    {
        add { this.hooks.ConfigChanged += value; }
        remove { this.hooks.ConfigChanged -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigCatClientErrorEventArgs>? Error
    {
        add { this.hooks.Error += value; }
        remove { this.hooks.Error -= value; }
    }

    private readonly struct SettingsWithRemoteConfig
    {
        public SettingsWithRemoteConfig(Dictionary<string, Setting>? value, ProjectConfig? remoteConfig)
        {
            Value = value;
            RemoteConfig = remoteConfig;
        }

        public Dictionary<string, Setting>? Value { get; }
        public ProjectConfig? RemoteConfig { get; }
    }
}
