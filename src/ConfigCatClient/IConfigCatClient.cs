using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client;

/// <summary>
/// Provides client definition for <see cref="ConfigCatClient"/>
/// </summary>
public interface IConfigCatClient : IProvidesHooks, IDisposable
{
    /// <summary>
    /// Sets or gets the logging level.
    /// </summary>
    LogLevel LogLevel { get; set; }

    /// <summary>
    /// Returns a value for the key. (Key for programs)
    /// </summary>
    /// <typeparam name="T">
    /// Setting type. Only the following types are allowed:
    /// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> and <see cref="object"/> (both nullable and non-nullable).
    /// </typeparam>
    /// <param name="key">Key for programs</param>
    /// <param name="defaultValue">In case of failure return this value</param>
    /// <param name="user">The user object for variation evaluation</param>
    /// <returns>The value of the feature flag or setting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
    T GetValue<T>(string key, T defaultValue, User? user = null);

    /// <summary>
    /// Returns a value for the key. (Key for programs)
    /// </summary>
    /// <typeparam name="T">
    /// Setting type. Only the following types are allowed:
    /// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> and <see cref="object"/> (both nullable and non-nullable).
    /// </typeparam>
    /// <param name="key">Key for programs.</param>
    /// <param name="defaultValue">In case of failure return this value.</param>
    /// <param name="user">The user object for variation evaluation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The task that will evaluate the value of the feature flag or setting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled during the execution of the task.</exception>
    Task<T> GetValueAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the value along with evaluation details of a feature flag or setting by the given key.
    /// </summary>
    /// <typeparam name="T">
    /// Setting type. Only the following types are allowed:
    /// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> and <see cref="object"/> (both nullable and non-nullable).
    /// </typeparam>
    /// <param name="key">Key for programs</param>
    /// <param name="defaultValue">In case of failure return this value</param>
    /// <param name="user">The user object for variation evaluation</param>
    /// <returns>The value along with the details of evaluation of the feature flag or setting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
    EvaluationDetails<T> GetValueDetails<T>(string key, T defaultValue, User? user = null);

    /// <summary>
    /// Returns the value along with evaluation details of a feature flag or setting by the given key.
    /// </summary>
    /// <typeparam name="T">
    /// Setting type. Only the following types are allowed:
    /// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> and <see cref="object"/> (both nullable and non-nullable).
    /// </typeparam>
    /// <param name="key">Key for programs</param>
    /// <param name="defaultValue">In case of failure return this value</param>
    /// <param name="user">The user object for variation evaluation</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The value along with the details of evaluation of the feature flag or setting.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled during the execution of the task.</exception>
    Task<EvaluationDetails<T>> GetValueDetailsAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a collection with all keys.
    /// </summary>
    /// <returns>The key collection.</returns>
    IReadOnlyCollection<string> GetAllKeys();

    /// <summary>
    /// Returns a collection with all keys asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The key collection.</returns>
    Task<IReadOnlyCollection<string>> GetAllKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the key-value collection of all feature flags and settings synchronously.
    /// </summary>
    /// <param name="user">The user object for variation evaluation.</param>
    /// <returns>The key-value collection.</returns>
    IReadOnlyDictionary<string, object?> GetAllValues(User? user = null);

    /// <summary>
    /// Returns the key-value collection of all feature flags and settings asynchronously.
    /// </summary>
    /// <param name="user">The user object for variation evaluation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The key-value collection.</returns>
    Task<IReadOnlyDictionary<string, object?>> GetAllValuesAsync(User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the values along with evaluation details of all feature flags and settings synchronously.
    /// </summary>
    /// <param name="user">The user object for variation evaluation.</param>
    /// <returns>The key-value collection.</returns>
    IReadOnlyList<EvaluationDetails> GetAllValueDetails(User? user = null);

    /// <summary>
    /// Returns the values along with evaluation details of all feature flags and settings asynchronously.
    /// </summary>
    /// <param name="user">The user object for variation evaluation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The key-value collection.</returns>
    Task<IReadOnlyList<EvaluationDetails>> GetAllValueDetailsAsync(User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the configuration.
    /// </summary>
    RefreshResult ForceRefresh();

    /// <summary>
    /// Refreshes the configuration asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    Task<RefreshResult> ForceRefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the default user.
    /// </summary>
    /// <param name="user">The default user object for variation evaluation.</param>
    void SetDefaultUser(User user);

    /// <summary>
    /// Sets the default user to null.
    /// </summary>
    void ClearDefaultUser();

    /// <summary>
    /// True when the client is configured not to initiate HTTP requests, otherwise false.
    /// </summary>
    bool IsOffline { get; }

    /// <summary>
    /// Configures the client to allow HTTP requests.
    /// </summary>
    void SetOnline();

    /// <summary>
    /// Configures the client to not initiate HTTP requests and work only from its cache.
    /// </summary>
    void SetOffline();
}
