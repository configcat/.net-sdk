using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Override;
using ConfigCat.Client.Shims;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// ConfigCat SDK client.
/// </summary>
public sealed class ConfigCatClient : IConfigCatClient
{
    private static readonly string Version = typeof(ConfigCatClient).GetTypeInfo().Assembly.GetName().Version!.ToString(fieldCount: 3);

    internal static readonly ConfigCatClientCache Instances = new();

#if NETSTANDARD
    /// <summary>
    /// Returns an object that can be used to configure the SDK to work on platforms that are not fully standards compliant.
    /// </summary>
    /// <remarks>
    /// Configuration is only possible before the first instance of <see cref="ConfigCatClient"/> is created.
    /// </remarks>
    public
#else
    internal
#endif
    static readonly PlatformCompatibilityOptions PlatformCompatibilityOptions = new();

    private readonly string? sdkKey; // may be null in case of testing
    private readonly EvaluationServices evaluationServices;
    private IConfigService configService;
    private readonly IOverrideDataSource? overrideDataSource;
    private readonly OverrideBehaviour? overrideBehaviour;
    private readonly Hooks hooks;
    // NOTE: The following mutable field(s) may be accessed from multiple threads and we need to make sure that changes to them are observable in these threads.
    // Volatile guarantees (see https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile) that such changes become visible to them *eventually*,
    // which is good enough in these cases.
    private volatile User? defaultUser;

    private LoggerWrapper Logger => this.evaluationServices.Logger;
    private IRolloutEvaluator ConfigEvaluator => this.evaluationServices.Evaluator;

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
        get => Logger.LogLevel;
        set => Logger.LogLevel = value;
    }

    internal ConfigCatClient(string sdkKey, ConfigCatClientOptions options)
    {
#if NETSTANDARD
        PlatformCompatibilityOptions.Freeze();
#endif

        this.sdkKey = sdkKey;

        this.hooks = options.YieldHooks();
        this.hooks.Sender = this;

        // To avoid possible memory leaks, the components of the client or client snapshots should not
        // hold a strong reference to the hooks object (see also SafeHooksWrapper).
        var hooksWrapper = new SafeHooksWrapper(this.hooks);

        var logger = new LoggerWrapper(options.Logger ?? ConfigCatClientOptions.CreateDefaultLogger(), options.LogFilter, hooksWrapper);
        var evaluator = new RolloutEvaluator(logger);

        this.evaluationServices = new EvaluationServices(evaluator, hooksWrapper, logger);

        var cacheParameters = new CacheParameters
        (
            configCache: options.ConfigCache is not null
                ? new ExternalConfigCache(options.ConfigCache, logger)
                : ConfigCatClientOptions.CreateDefaultConfigCache(),
            cacheKey: GetCacheKey(sdkKey)
        );

        if (options.FlagOverrides is not null)
        {
            this.overrideDataSource = options.FlagOverrides.BuildDataSource(logger);
            this.overrideBehaviour = options.FlagOverrides.OverrideBehaviour;
        }

        this.defaultUser = options.DefaultUser;

        var pollingMode = options.PollingMode ?? ConfigCatClientOptions.CreateDefaultPollingMode();

        // At this point the client instance must be fully initialized (apart from the configService field) because it
        // may be accessed from a handler of an event that is raised during the initialization of the config service.
        // (At the same time, the config service must initialize the configService field before raising any events.)
        var configService = this.overrideBehaviour != OverrideBehaviour.LocalOnly
            ? pollingMode.CreateConfigService(
                new DefaultConfigFetcher(sdkKey,
                    options.GetBaseUri(),
                    GetProductVersion(pollingMode),
                    logger,
                    options.ConfigFetcher
                        ?? PlatformCompatibilityOptions.configFetcherFactory?.Invoke(options.HttpClientHandler)
                        ?? ConfigCatClientOptions.CreateDefaultConfigFetcher(options.HttpClientHandler),
                    options.IsCustomBaseUrl,
                    options.HttpTimeout),
                cacheParameters,
                logger,
                options.Offline,
                hooksWrapper)
            : new NullConfigService(logger, hooksWrapper);

        Debug.Assert(ReferenceEquals(this.configService, configService) || this.hooks.Sender is null, $"The {nameof(this.configService)} field was not initialized.");

        this.configService = configService;
    }

    internal void InitializeConfigService(IConfigService instance)
    {
        Debug.Assert(this.configService is null, $"The {nameof(this.configService)} field was already initialized.");
        this.configService = instance;
    }

    // For test purposes only
    internal ConfigCatClient(IConfigService configService, IConfigCatLogger logger, IRolloutEvaluator evaluator,
        OverrideBehaviour? overrideBehaviour = null, IOverrideDataSource? overrideDataSource = null,
        LogFilterCallback? logFilter = null, Hooks? hooks = null)
    {
#if NETSTANDARD
        PlatformCompatibilityOptions.Freeze();
#endif

        this.hooks = hooks ?? NullHooks.Instance;
        this.hooks.Sender = this;
        var hooksWrapper = new SafeHooksWrapper(this.hooks);

        this.evaluationServices = new EvaluationServices(evaluator, hooksWrapper, new LoggerWrapper(logger, logFilter, hooks));

        this.configService = configService;

        this.overrideBehaviour = overrideBehaviour;
        this.overrideDataSource = overrideDataSource;
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
            instance.Logger.ClientIsAlreadyCreated(sdkKey);
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
            (this.configService as IDisposable)?.Dispose();
            (this.overrideDataSource as IDisposable)?.Dispose();
        }
        else
        {
            // Execution gets here when consumer forgets to dispose the client instance.
            // In this case we need to make sure that background work is stopped,
            // otherwise it would go on endlessly, that is, we'd end up with a memory leak.
            var configService = this.configService as IDisposable;
            var localFileDataSource = this.overrideDataSource as IDisposable;
            if (configService is not null || localFileDataSource is not null)
            {
                TaskShim.Current.Run(() =>
                {
                    configService?.Dispose();
                    localFileDataSource?.Dispose();
                    return TaskShim.CompletedTask;
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
    public async ValueTask<T> GetValueAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default)
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
            settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            evaluationDetails = ConfigEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, Logger);
            value = evaluationDetails.Value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.SettingEvaluationError(nameof(GetValueAsync), key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user,
                ex.Message, ex, EvaluationHelper.GetErrorCode(ex));
            value = defaultValue;
        }

        this.hooks.RaiseFlagEvaluated(evaluationDetails);
        return value;
    }

    /// <inheritdoc />
    public async ValueTask<EvaluationDetails<T>> GetValueDetailsAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default)
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
            settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            evaluationDetails = ConfigEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, Logger);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.SettingEvaluationError(nameof(GetValueDetailsAsync), key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user,
                ex.Message, ex, EvaluationHelper.GetErrorCode(ex));
        }

        this.hooks.RaiseFlagEvaluated(evaluationDetails);
        return evaluationDetails;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyCollection<string>> GetAllKeysAsync(CancellationToken cancellationToken = default)
    {
        const string defaultReturnValue = "empty collection";
        try
        {
            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            if (!EvaluationHelper.CheckSettingsAvailable(settings.Value, Logger, defaultReturnValue))
            {
                return Array.Empty<string>();
            }
            return settings.Value.KeyCollection();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.SettingEvaluationError(nameof(GetAllKeysAsync), defaultReturnValue, ex);
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<string, object?>> GetAllValuesAsync(User? user = null, CancellationToken cancellationToken = default)
    {
        const string defaultReturnValue = "empty dictionary";
        Dictionary<string, object?> result;
        EvaluationDetails[]? evaluationDetailsArray;
        IReadOnlyList<Exception>? evaluationExceptions;
        user ??= this.defaultUser;
        try
        {
            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            evaluationDetailsArray = ConfigEvaluator.EvaluateAll(settings.Value, user, settings.RemoteConfig, Logger, defaultReturnValue, out evaluationExceptions);
            result = evaluationDetailsArray.ToDictionary(details => details.Key, details => details.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.SettingEvaluationError(nameof(GetAllValuesAsync), defaultReturnValue, ex);
            return new Dictionary<string, object?>();
        }

        if (evaluationExceptions is { Count: > 0 })
        {
            Logger.SettingEvaluationError(nameof(GetAllValuesAsync), "evaluation result", new AggregateException(evaluationExceptions));
        }

        foreach (var evaluationDetails in evaluationDetailsArray)
        {
            this.hooks.RaiseFlagEvaluated(evaluationDetails);
        }

        return result;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<EvaluationDetails>> GetAllValueDetailsAsync(User? user = null, CancellationToken cancellationToken = default)
    {
        const string defaultReturnValue = "empty list";
        EvaluationDetails[]? evaluationDetailsArray;
        IReadOnlyList<Exception>? evaluationExceptions;
        user ??= this.defaultUser;
        try
        {
            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            evaluationDetailsArray = ConfigEvaluator.EvaluateAll(settings.Value, user, settings.RemoteConfig, Logger, defaultReturnValue, out evaluationExceptions);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.SettingEvaluationError(nameof(GetAllValueDetailsAsync), defaultReturnValue, ex);
            return Array.Empty<EvaluationDetails>();
        }

        if (evaluationExceptions is { Count: > 0 })
        {
            Logger.SettingEvaluationError(nameof(GetAllValueDetailsAsync), "evaluation result", new AggregateException(evaluationExceptions));
        }

        foreach (var evaluationDetails in evaluationDetailsArray)
        {
            this.hooks.RaiseFlagEvaluated(evaluationDetails);
        }

        return evaluationDetailsArray;
    }

    /// <inheritdoc />
    public async ValueTask<KeyValuePair<string, T>?> GetKeyAndValueAsync<T>(string variationId, CancellationToken cancellationToken = default)
    {
        if (variationId is null)
        {
            throw new ArgumentNullException(nameof(variationId));
        }

        if (variationId.Length == 0)
        {
            throw new ArgumentException("Variation ID cannot be empty.", nameof(variationId));
        }

        typeof(T).EnsureSupportedSettingClrType(nameof(T));

        const string defaultReturnValue = "null";
        try
        {
            var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            return EvaluationHelper.GetKeyAndValue<T>(settings.Value, variationId, Logger, defaultReturnValue);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.SettingEvaluationError(nameof(GetKeyAndValueAsync), defaultReturnValue, ex);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<RefreshResult> ForceRefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await this.configService.RefreshConfigAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.ClientMethodError(nameof(ForceRefreshAsync), ex);
            return RefreshResult.Failure(RefreshErrorCode.UnexpectedError, ex.Message, ex);
        }
    }

    private SettingsWithRemoteConfig GetInMemorySettings()
    {
        IReadOnlyDictionary<string, Setting> local;
        SettingsWithRemoteConfig remote;
        switch (this.overrideBehaviour)
        {
            case null:
                return GetRemoteConfig();
            case OverrideBehaviour.LocalOnly:
                local = this.overrideDataSource!.GetOverrides();
                return new SettingsWithRemoteConfig(local, remoteConfig: null);
            case OverrideBehaviour.LocalOverRemote:
                local = this.overrideDataSource!.GetOverrides();
                remote = GetRemoteConfig();
                return new SettingsWithRemoteConfig(remote.Value is { Count: > 0 } ? remote.Value.MergeOverwriteWith(local) : local, remoteConfig: null);
            case OverrideBehaviour.RemoteOverLocal:
                local = this.overrideDataSource!.GetOverrides();
                remote = GetRemoteConfig();
                return new SettingsWithRemoteConfig(remote.Value is { Count: > 0 } ? local.MergeOverwriteWith(remote.Value) : local, remote.RemoteConfig);
            default:
                throw new InvalidOperationException(); // execution should never get here
        }

        SettingsWithRemoteConfig GetRemoteConfig()
        {
            var config = this.configService.GetInMemoryConfig();
            var settings = !config.IsEmpty ? config.Config.SettingsOrEmpty : null;
            return new SettingsWithRemoteConfig(settings, config);
        }
    }

    private async ValueTask<SettingsWithRemoteConfig> GetSettingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<string, Setting> local;
        SettingsWithRemoteConfig remote;
        switch (this.overrideBehaviour)
        {
            case null:
                return await GetRemoteConfigAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            case OverrideBehaviour.LocalOnly:
                local = this.overrideDataSource!.GetOverrides();
                return new SettingsWithRemoteConfig(local, remoteConfig: null);
            case OverrideBehaviour.LocalOverRemote:
                local = this.overrideDataSource!.GetOverrides();
                remote = await GetRemoteConfigAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                return new SettingsWithRemoteConfig(remote.Value is { Count: > 0 } ? remote.Value.MergeOverwriteWith(local) : local, remoteConfig: null);
            case OverrideBehaviour.RemoteOverLocal:
                local = this.overrideDataSource!.GetOverrides();
                remote = await GetRemoteConfigAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                return new SettingsWithRemoteConfig(remote.Value is { Count: > 0 } ? local.MergeOverwriteWith(remote.Value) : local, remote.RemoteConfig);
            default:
                throw new InvalidOperationException(); // execution should never get here
        }

        async ValueTask<SettingsWithRemoteConfig> GetRemoteConfigAsync(CancellationToken cancellationToken)
        {
            var config = await this.configService.GetConfigAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            var settings = !config.IsEmpty ? config.Config.SettingsOrEmpty : null;
            return new SettingsWithRemoteConfig(settings, config);
        }
    }

    /// <inheritdoc />
    public void SetDefaultUser(User user)
    {
        this.defaultUser = user ?? throw new ArgumentNullException(nameof(user));
    }

    /// <inheritdoc />
    public Task<ClientCacheState> WaitForReadyAsync(CancellationToken cancellationToken = default)
    {
        return this.configService.ReadyTask.WaitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public ConfigCatClientSnapshot Snapshot()
    {
        SettingsWithRemoteConfig settings;
        try
        {
            settings = GetInMemorySettings();
            var cacheState = this.configService.GetCacheState(settings.RemoteConfig ?? ProjectConfig.Empty);
            return new ConfigCatClientSnapshot(this.evaluationServices, settings, this.defaultUser, cacheState);
        }
        catch (Exception ex)
        {
            Logger.ClientMethodError(nameof(Snapshot), ex);
            settings = new SettingsWithRemoteConfig(new Dictionary<string, Setting>(), remoteConfig: null);
            return new ConfigCatClientSnapshot(this.evaluationServices, settings, this.defaultUser, this.configService.GetCacheState(ProjectConfig.Empty));
        }
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
    public event EventHandler<ClientReadyEventArgs>? ClientReady
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
    public event EventHandler<ConfigFetchedEventArgs>? ConfigFetched
    {
        add { this.hooks.ConfigFetched += value; }
        remove { this.hooks.ConfigFetched -= value; }
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

    internal readonly struct SettingsWithRemoteConfig
    {
        public SettingsWithRemoteConfig(IReadOnlyDictionary<string, Setting>? value, ProjectConfig? remoteConfig)
        {
            Value = value;
            RemoteConfig = remoteConfig;
        }

        public IReadOnlyDictionary<string, Setting>? Value { get; }
        public ProjectConfig? RemoteConfig { get; }
    }

    internal sealed class EvaluationServices
    {
        public EvaluationServices(IRolloutEvaluator evaluator, SafeHooksWrapper hooks, LoggerWrapper logger)
        {
            this.Evaluator = evaluator;
            this.Hooks = hooks;
            this.Logger = logger;
        }

        public readonly IRolloutEvaluator Evaluator;
        public readonly SafeHooksWrapper Hooks;
        public readonly LoggerWrapper Logger;
    }
}
