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
    /// Returns the value of a feature flag or setting identified by <paramref name="key"/> synchronously.
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
    T GetValue<T>(string key, T defaultValue, User? user = null);

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
    Task<T> GetValueAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the value along with evaluation details of a feature flag or setting identified by <paramref name="key"/> synchronously.
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
    EvaluationDetails<T> GetValueDetails<T>(string key, T defaultValue, User? user = null);

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
    Task<EvaluationDetails<T>> GetValueDetailsAsync<T>(string key, T defaultValue, User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all setting keys synchronously.
    /// </summary>
    /// <returns>The collection of keys.</returns>
    IReadOnlyCollection<string> GetAllKeys();

    /// <summary>
    /// Returns all setting keys asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the collection of keys.</returns>
    Task<IReadOnlyCollection<string>> GetAllKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the keys and values of all feature flags and settings synchronously.
    /// </summary>
    /// <param name="user">The User Object to use for evaluating targeting rules and percentage options.</param>
    /// <returns>The dictionary containing the keys and values.</returns>
    IReadOnlyDictionary<string, object?> GetAllValues(User? user = null);

    /// <summary>
    /// Returns the keys and values of all feature flags and settings asynchronously.
    /// </summary>
    /// <param name="user">The User Object to use for evaluating targeting rules and percentage options.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the dictionary containing the keys and values.</returns>
    Task<IReadOnlyDictionary<string, object?>> GetAllValuesAsync(User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the values along with evaluation details of all feature flags and settings synchronously.
    /// </summary>
    /// <param name="user">The User Object to use for evaluating targeting rules and percentage options.</param>
    /// <returns>The list of values along with evaluation details.</returns>
    IReadOnlyList<EvaluationDetails> GetAllValueDetails(User? user = null);

    /// <summary>
    /// Returns the values along with evaluation details of all feature flags and settings asynchronously.
    /// </summary>
    /// <param name="user">The User Object to use for evaluating targeting rules and percentage options.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of values along with evaluation details.</returns>
    Task<IReadOnlyList<EvaluationDetails>> GetAllValueDetailsAsync(User? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the locally cached config by fetching the latest version from the remote server synchronously.
    /// </summary>
    /// <returns>The refresh result.</returns>
    RefreshResult ForceRefresh();

    /// <summary>
    /// Refreshes the locally cached config by fetching the latest version from the remote server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the refresh result.</returns>
    Task<RefreshResult> ForceRefreshAsync(CancellationToken cancellationToken = default);

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
    /// Configures the client to not initiate HTTP requests and work using the locally cached config only.
    /// </summary>
    void SetOffline();
}
