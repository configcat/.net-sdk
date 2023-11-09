using System;
using System.Collections.Generic;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

using static ConfigCat.Client.ConfigCatClient;

namespace ConfigCat.Client;

/// <summary>
/// Represents the state of <see cref="IConfigCatClient"/> captured at a specific point in time.
/// </summary>
public readonly struct ConfigCatClientSnapshot
{
    private readonly EvaluationServices evaluationServices;
    private readonly SettingsWithRemoteConfig settings;
    private readonly User? defaultUser;

    private LoggerWrapper Logger => this.evaluationServices.Logger;
    private IRolloutEvaluator ConfigEvaluator => this.evaluationServices.Evaluator;
    private SafeHooksWrapper Hooks => this.evaluationServices.Hooks;

    internal ConfigCatClientSnapshot(EvaluationServices evaluationServices, SettingsWithRemoteConfig settings, User? defaultUser, ClientCacheState cacheState)
    {
        this.evaluationServices = evaluationServices;
        this.settings = settings;
        this.defaultUser = defaultUser;
        CacheState = cacheState;
    }

    /// <summary>
    /// The state of the local cache at the time the snapshot was created.
    /// </summary>
    public ClientCacheState CacheState { get; }

    /// <summary>
    /// The latest config which has been fetched from the remote server.
    /// </summary>
    public IConfig? FetchedConfig => this.settings.RemoteConfig?.Config;

    /// <summary>
    /// Returns the available setting keys.
    /// </summary>
    /// <remarks>
    /// In case the client is configured to use flag override, this will also include the keys provided by the flag override.
    /// </remarks>
    /// <returns>The collection of keys.</returns>
    public IReadOnlyCollection<string> GetAllKeys()
    {
        return this.settings.Value is { } settings ? settings.ReadOnlyKeys() : ArrayUtils.EmptyArray<string>();
    }

    /// <summary>
    /// Returns the value of a feature flag or setting identified by <paramref name="key"/> synchronously, based on the snapshot.
    /// </summary>
    /// <remarks>
    /// It is important to provide an argument for the <paramref name="defaultValue"/> parameter, specifically for the <typeparamref name="T"/> generic type parameter,
    /// that matches the type of the feature flag or setting you are evaluating.<br/>
    /// Please refer to <see href="https://configcat.com/docs/sdk-reference/dotnet/#setting-type-mapping">this table</see> for the corresponding types.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of the value. Only the following types are allowed:
    /// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> and <see cref="object"/> (both nullable and non-nullable).<br/>
    /// The type must correspond to the setting type, otherwise <paramref name="defaultValue"/> will be returned.
    /// </typeparam>
    /// <param name="key">Key of the feature flag or setting.</param>
    /// <param name="defaultValue">In case of failure, this value will be returned.</param>
    /// <param name="user">The User Object to use for evaluating targeting rules and percentage options.</param>
    /// <returns>The value of the feature flag or setting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
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
            settings = this.settings;
            evaluationDetails = ConfigEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, Logger);
            value = evaluationDetails.Value;
        }
        catch (Exception ex)
        {
            Logger.SettingEvaluationError($"{nameof(ConfigCatClientSnapshot)}.{nameof(GetValue)}", key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
            value = defaultValue;
        }

        Hooks.RaiseFlagEvaluated(evaluationDetails);
        return value;
    }

    /// <summary>
    /// Returns the value along with evaluation details of a feature flag or setting identified by <paramref name="key"/> synchronously, based on the snapshot.
    /// </summary>
    /// <remarks>
    /// It is important to provide an argument for the <paramref name="defaultValue"/> parameter, specifically for the <typeparamref name="T"/> generic type parameter,
    /// that matches the type of the feature flag or setting you are evaluating.<br/>
    /// Please refer to <see href="https://configcat.com/docs/sdk-reference/dotnet/#setting-type-mapping">this table</see> for the corresponding types.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of the value. Only the following types are allowed:
    /// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> and <see cref="object"/> (both nullable and non-nullable).<br/>
    /// The type must correspond to the setting type, otherwise <paramref name="defaultValue"/> will be returned.
    /// </typeparam>
    /// <param name="key">Key of the feature flag or setting.</param>
    /// <param name="defaultValue">In case of failure, this value will be returned.</param>
    /// <param name="user">The User Object to use for evaluating targeting rules and percentage options.</param>
    /// <returns>The value along with the details of evaluation of the feature flag or setting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
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
            settings = this.settings;
            evaluationDetails = ConfigEvaluator.Evaluate(settings.Value, key, defaultValue, user, settings.RemoteConfig, Logger);
        }
        catch (Exception ex)
        {
            Logger.SettingEvaluationError($"{nameof(ConfigCatClientSnapshot)}.{nameof(GetValueDetails)}", key, nameof(defaultValue), defaultValue, ex);
            evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: settings.RemoteConfig?.TimeStamp, user, ex.Message, ex);
        }

        Hooks.RaiseFlagEvaluated(evaluationDetails);
        return evaluationDetails;
    }
}
