using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    /// <summary>
    /// Provides client definition for <see cref="ConfigCatClient"/>
    /// </summary>
    public interface IConfigCatClient : IDisposable
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
        T GetValue<T>(string key, T defaultValue, User user = null);

        /// <summary>
        /// Returns a value for the key. (Key for programs)
        /// </summary>
        /// <typeparam name="T">Setting type.</typeparam>
        /// <param name="key">Key for programs.</param>
        /// <param name="defaultValue">In case of failure return this value.</param>
        /// <param name="user">The user object for variation evaluation.</param>
        /// <returns>The task that will evaluate the value of the feature flag or setting.</returns>
        Task<T> GetValueAsync<T>(string key, T defaultValue, User user = null);

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
        IDictionary<string, object> GetAllValues(User user = null);

        /// <summary>
        /// Returns the key-value collection of all feature flags and settings asynchronously.
        /// </summary>
        /// <param name="user">The user object for variation evaluation.</param>
        /// <returns>The key-value collection.</returns>
        Task<IDictionary<string, object>> GetAllValuesAsync(User user = null);

        /// <summary>
        /// Refreshes the configuration.
        /// </summary>
        void ForceRefresh();

        /// <summary>
        /// Refreshes the configuration asynchronously.
        /// </summary>
        Task ForceRefreshAsync();

        /// <summary>
        /// Returns the Variation ID (analytics) for a feature flag or setting by the given key.
        /// </summary>
        /// <param name="key">Key for programs</param>
        /// <param name="defaultVariationId">In case of failure return this value.</param>
        /// <param name="user">The user object for variation evaluation.</param>
        /// <returns>Variation ID.</returns>
        string GetVariationId(string key, string defaultVariationId, User user = null);

        /// <summary>
        /// Returns the Variation ID (analytics) for a feature flag or setting by the given key.
        /// </summary>
        /// <param name="key">Key for programs</param>
        /// <param name="defaultVariationId">In case of failure return this value.</param>
        /// <param name="user">The user object for variation evaluation.</param>
        /// <returns>Variation ID.</returns>
        Task<string> GetVariationIdAsync(string key, string defaultVariationId, User user = null);

        /// <summary>
        /// Returns all Variation IDs (analytics) for each feature flag and setting.
        /// </summary>
        /// <param name="user">The user object for variation evaluation.</param>
        /// <returns>Collection of all Variation IDs.</returns>
        IEnumerable<string> GetAllVariationId(User user = null);

        /// <summary>
        /// Returns all Variation IDs (analytics) for each feature flags or settings.
        /// </summary>
        /// <param name="user">The user object for variation evaluation.</param>
        /// <returns>Collection of all Variation IDs.</returns>
        Task<IEnumerable<string>> GetAllVariationIdAsync(User user = null);
    }
}