using System;
using System.Collections.Generic;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

using static ConfigCat.Client.ConfigCatClient;

namespace ConfigCat.Client;

/// <summary>
/// Represents the state of <see cref="IConfigCatClient"/> captured at a specific point in time.
/// </summary>
public readonly struct ConfigCatClientSnapshot : IConfigCatClientSnapshot
{
    private readonly object? evaluationServicesOrFakeImpl; // an instance of either EvaluationServices or IConfigCatClientSnapshot
    private readonly SettingsWithRemoteConfig settings;
    private readonly User? defaultUser;
    private readonly ClientCacheState cacheState;

    internal ConfigCatClientSnapshot(EvaluationServices evaluationServices, SettingsWithRemoteConfig settings, User? defaultUser, ClientCacheState cacheState)
    {
        this.evaluationServicesOrFakeImpl = evaluationServices;
        this.settings = settings;
        this.defaultUser = defaultUser;
        this.cacheState = cacheState;
    }

    /// <summary>
    /// For testing purposes. This constructor allows you to create an instance
    /// which will use the fake implementation you provide instead of executing the built-in logic.
    /// </summary>
    public ConfigCatClientSnapshot(IConfigCatClientSnapshot impl)
    {
        this.evaluationServicesOrFakeImpl = impl;
        this.settings = default;
        this.defaultUser = default;
        this.cacheState = default;
    }

    /// <inheritdoc/>>
    public ClientCacheState CacheState => this.evaluationServicesOrFakeImpl is EvaluationServices
        ? this.cacheState
        : ((IConfigCatClientSnapshot?)this.evaluationServicesOrFakeImpl)?.CacheState ?? ClientCacheState.NoFlagData;

    /// <inheritdoc/>>
    public IConfig? FetchedConfig => this.evaluationServicesOrFakeImpl is EvaluationServices
        ? this.settings.RemoteConfig?.Config
        : ((IConfigCatClientSnapshot?)this.evaluationServicesOrFakeImpl)?.FetchedConfig ?? null;

    /// <inheritdoc/>>
    public IReadOnlyCollection<string> GetAllKeys()
    {
        if (this.evaluationServicesOrFakeImpl is not EvaluationServices)
        {
            return this.evaluationServicesOrFakeImpl is not null
                ? ((IConfigCatClientSnapshot)this.evaluationServicesOrFakeImpl).GetAllKeys()
                : ArrayUtils.EmptyArray<string>();
        }

        return this.settings.Value is { } settings ? settings.ReadOnlyKeys() : ArrayUtils.EmptyArray<string>();
    }

    /// <inheritdoc/>>
    public T GetValue<T>(string key, T defaultValue, User? user = null)
    {
        if (this.evaluationServicesOrFakeImpl is not EvaluationServices evaluationServices)
        {
            return this.evaluationServicesOrFakeImpl is not null
                ? ((IConfigCatClientSnapshot)this.evaluationServicesOrFakeImpl).GetValue(key, defaultValue, user)
                : defaultValue;
        }

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
            settings = this.settings;
            evaluationDetails = evaluationServices.Evaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, evaluationServices.Logger);
            value = evaluationDetails.Value;
        }
        catch (Exception ex)
        {
            evaluationServices.Logger.SettingEvaluationError($"{nameof(ConfigCatClientSnapshot)}.{nameof(GetValue)}", key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
            value = defaultValue;
        }

        evaluationServices.Hooks.RaiseFlagEvaluated(evaluationDetails);
        return value;
    }

    /// <inheritdoc/>>
    public EvaluationDetails<T> GetValueDetails<T>(string key, T defaultValue, User? user = null)
    {
        if (this.evaluationServicesOrFakeImpl is not EvaluationServices evaluationServices)
        {
            return this.evaluationServicesOrFakeImpl is not null
                ? ((IConfigCatClientSnapshot)this.evaluationServicesOrFakeImpl).GetValueDetails(key, defaultValue, user)
                : EvaluationDetails.FromDefaultValue(key, defaultValue, null, user, $"{nameof(GetValueDetails)} was called on the default instance of {nameof(ConfigCatClientSnapshot)}.");
        }

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
            settings = this.settings;
            evaluationDetails = evaluationServices.Evaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, evaluationServices.Logger);
        }
        catch (Exception ex)
        {
            evaluationServices.Logger.SettingEvaluationError($"{nameof(ConfigCatClientSnapshot)}.{nameof(GetValueDetails)}", key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
        }

        evaluationServices.Hooks.RaiseFlagEvaluated(evaluationDetails);
        return evaluationDetails;
    }
}
