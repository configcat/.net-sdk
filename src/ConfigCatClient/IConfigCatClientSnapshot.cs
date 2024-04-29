using System;
using System.Collections.Generic;

namespace ConfigCat.Client;

/// <summary>
/// Defines the public interface of the <see cref="ConfigCatClientSnapshot"/> struct.
/// </summary>
public interface IConfigCatClientSnapshot
{
    /// <summary>
    /// The state of the local cache at the time the snapshot was created.
    /// </summary>
    ClientCacheState CacheState { get; }

    /// <summary>
    /// The latest config which has been fetched from the remote server.
    /// </summary>
    IConfig? FetchedConfig { get; }

    /// <summary>
    /// Returns the available setting keys.
    /// </summary>
    /// <remarks>
    /// In case the client is configured to use flag override, this will also include the keys provided by the flag override.
    /// </remarks>
    /// <returns>The collection of keys.</returns>
    IReadOnlyCollection<string> GetAllKeys();

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
    T GetValue<T>(string key, T defaultValue, User? user = null);

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
    EvaluationDetails<T> GetValueDetails<T>(string key, T defaultValue, User? user = null);

    /// <summary>
    /// Returns the key of a feature flag or setting and its value identified by the given Variation ID (analytics) synchronously, based on the snapshot.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value. Only the following types are allowed:
    /// <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> and <see cref="object"/> (both nullable and non-nullable).<br/>
    /// The type must correspond to the setting type, otherwise <see langword="null"/> will be returned.
    /// </typeparam>
    /// <param name="variationId">The Variation ID.</param>
    /// <returns>The key of the feature flag or setting and its value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="variationId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="variationId"/> is an empty string.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is not an allowed type.</exception>
    KeyValuePair<string, T>? GetKeyAndValue<T>(string variationId);
}
