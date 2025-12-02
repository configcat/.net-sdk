using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client;

/// <summary>
/// Defines the public interface of the <see cref="ConfigCatClient"/> class.
/// </summary>
public interface IConfigCatClient : IProvidesHooks, IDisposable
{
    /// <summary>
    /// Gets or sets the log level (the minimum level to use for filtering log events).
    /// </summary>
    LogLevel LogLevel { get; set; }

    /// <summary>
    /// Returns the value of a feature flag or setting identified by <paramref name="key"/> asynchronously.
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
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the value of the feature flag or setting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled during the execution of the task.</exception>
    ValueTask<T> GetValueAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the value along with evaluation details of a feature flag or setting identified by <paramref name="key"/> asynchronously.
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
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the value along with the details of evaluation of the feature flag or setting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled during the execution of the task.</exception>
    ValueTask<EvaluationDetails<T>> GetValueDetailsAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all setting keys asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the collection of keys.</returns>
    ValueTask<IReadOnlyCollection<string>> GetAllKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the keys and values of all feature flags and settings asynchronously.
    /// </summary>
    /// <param name="user">The User Object to use for evaluating targeting rules and percentage options.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the dictionary containing the keys and values.</returns>
    ValueTask<IReadOnlyDictionary<string, object?>> GetAllValuesAsync(User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the values along with evaluation details of all feature flags and settings asynchronously.
    /// </summary>
    /// <param name="user">The User Object to use for evaluating targeting rules and percentage options.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of values along with evaluation details.</returns>
    ValueTask<IReadOnlyList<EvaluationDetails>> GetAllValueDetailsAsync(User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the key of a feature flag or setting and its value identified by the given Variation ID (analytics) asynchronously.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value. Only the following types are allowed:
    /// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> and <see cref="object"/> (both nullable and non-nullable).<br/>
    /// The type must correspond to the setting type, otherwise <see langword="null"/> will be returned.
    /// </typeparam>
    /// <param name="variationId">The Variation ID.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the key of the feature flag or setting and its value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="variationId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="variationId"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled during the execution of the task.</exception>
    ValueTask<KeyValuePair<string, T>?> GetKeyAndValueAsync<T>(string variationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the internally cached config by synchronizing with the external cache (if any),
    /// then by fetching the latest version from the ConfigCat CDN asynchronously (provided that the client is online).
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the refresh result.</returns>
    Task<RefreshResult> ForceRefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for the client to reach the ready state, i.e. to complete initialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ready state is reached as soon as the initial sync with the external cache (if any) completes.
    /// If this does not produce up-to-date config data, and the client is online (i.e. HTTP requests are allowed),
    /// the first config fetch operation is also awaited in Auto Polling mode before ready state is reported.
    /// </para>
    /// <para>
    /// That is, reaching the ready state usually means the client is ready to evaluate feature flags and settings.
    /// However, please note that this is not guaranteed. In case of initialization failure or timeout, the internal cache
    /// may be empty or expired even after the ready state is reported. You can verify this by checking the return value.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the state of the internal cache at the time initialization was completed.</returns>
    Task<ClientCacheState> WaitForReadyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures the current state of the client.
    /// The resulting snapshot can be used to evaluate feature flags and settings based on the captured state synchronously,
    /// without any underlying I/O-bound operations, which could block the executing thread for a longer period of time.
    /// </summary>
    /// <remarks>
    /// The operation captures the internally cached config data. It does not attempt to update it by synchronizing with
    /// the external cache or by fetching the latest version from the ConfigCat CDN.<br/>
    /// Therefore, it is recommended to use snapshots in conjunction with the Auto Polling mode, where the SDK automatically updates the internal cache in the background.<br/>
    /// For other polling modes, you will need to manually initiate a cache update by invoking <see cref="ForceRefreshAsync"/>.
    /// </remarks>
    /// <returns>The snapshot object.</returns>
    ConfigCatClientSnapshot Snapshot();

    /// <summary>
    /// Sets the default user.
    /// </summary>
    /// <param name="user">The default User Object to use for evaluating targeting rules and percentage options.</param>
    void SetDefaultUser(User user);

    /// <summary>
    /// Clears the default user.
    /// </summary>
    void ClearDefaultUser();

    /// <summary>
    /// Returns <see langword="true"/> when the client is configured not to initiate HTTP requests, otherwise <see langword="false"/>.
    /// </summary>
    bool IsOffline { get; }

    /// <summary>
    /// Configures the client to allow HTTP requests.
    /// </summary>
    void SetOnline();

    /// <summary>
    /// Configures the client to not initiate HTTP requests but work using the cache only.
    /// </summary>
    void SetOffline();
}
