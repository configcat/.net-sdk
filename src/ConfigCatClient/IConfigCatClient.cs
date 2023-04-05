using System;
using System.Collections.Generic;
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
    /// <typeparam name="T">Setting type</typeparam>
    /// <param name="key">Key for programs</param>
    /// <param name="defaultValue">In case of failure return this value</param>
    /// <param name="user">The user object for variation evaluation</param>
    /// <returns>The value of the feature flag or setting.</returns>
    T GetValue<T>(string key, T defaultValue, User? user = null);

    /// <summary>
    /// Returns a value for the key. (Key for programs)
    /// </summary>
    /// <typeparam name="T">Setting type.</typeparam>
    /// <param name="key">Key for programs.</param>
    /// <param name="defaultValue">In case of failure return this value.</param>
    /// <param name="user">The user object for variation evaluation.</param>
    /// <returns>The task that will evaluate the value of the feature flag or setting.</returns>
    Task<T> GetValueAsync<T>(string key, T defaultValue, User? user = null);

    /// <summary>
    /// Returns the value along with evaluation details of a feature flag or setting by the given key.
    /// </summary>
    /// <typeparam name="T">Setting type</typeparam>
    /// <param name="key">Key for programs</param>
    /// <param name="defaultValue">In case of failure return this value</param>
    /// <param name="user">The user object for variation evaluation</param>
    /// <returns>The value along with the details of evaluation of the feature flag or setting.</returns>
    EvaluationDetails<T> GetValueDetails<T>(string key, T defaultValue, User? user = null);

    /// <summary>
    /// Returns the value along with evaluation details of a feature flag or setting by the given key.
    /// </summary>
    /// <typeparam name="T">Setting type</typeparam>
    /// <param name="key">Key for programs</param>
    /// <param name="defaultValue">In case of failure return this value</param>
    /// <param name="user">The user object for variation evaluation</param>
    /// <returns>The value along with the details of evaluation of the feature flag or setting.</returns>
    Task<EvaluationDetails<T>> GetValueDetailsAsync<T>(string key, T defaultValue, User? user = null);

    /// <summary>
    /// Returns a collection with all keys.
    /// </summary>
    /// <returns>The key collection.</returns>
    IEnumerable<string> GetAllKeys();

    /// <summary>
    /// Returns a collection with all keys asynchronously.
    /// </summary>
    /// <returns>The key collection.</returns>
    Task<IEnumerable<string>> GetAllKeysAsync();

    /// <summary>
    /// Returns the key-value collection of all feature flags and settings synchronously.
    /// </summary>
    /// <param name="user">The user object for variation evaluation.</param>
    /// <returns>The key-value collection.</returns>
    IDictionary<string, object?> GetAllValues(User? user = null);

    /// <summary>
    /// Returns the key-value collection of all feature flags and settings asynchronously.
    /// </summary>
    /// <param name="user">The user object for variation evaluation.</param>
    /// <returns>The key-value collection.</returns>
    Task<IDictionary<string, object?>> GetAllValuesAsync(User? user = null);

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
    /// <returns>The key-value collection.</returns>
    Task<IReadOnlyList<EvaluationDetails>> GetAllValueDetailsAsync(User? user = null);

    /// <summary>
    /// Refreshes the configuration.
    /// </summary>
    RefreshResult ForceRefresh();

    /// <summary>
    /// Refreshes the configuration asynchronously.
    /// </summary>
    Task<RefreshResult> ForceRefreshAsync();

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
