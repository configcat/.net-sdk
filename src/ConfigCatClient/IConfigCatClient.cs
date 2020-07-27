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
        /// Set or get logging level
        /// </summary>
        LogLevel LogLevel { get; set; }

        /// <summary>
        /// Return a value of the key (Key for programs)
        /// </summary>
        /// <typeparam name="T">Setting type</typeparam>
        /// <param name="key">Key for programs</param>
        /// <param name="defaultValue">In case of failure return this value</param>
        /// <param name="user">The user object for variation evaluation</param>
        /// <returns></returns>
        T GetValue<T>(string key, T defaultValue, User user = null);

        /// <summary>
        /// Return a value of the key (Key for programs)
        /// </summary>
        /// <typeparam name="T">Setting type</typeparam>
        /// <param name="key">Key for programs</param>
        /// <param name="defaultValue">In case of failure return this value</param>
        /// <param name="user">The user object for variation evaluation</param>
        /// <returns></returns>
        Task<T> GetValueAsync<T>(string key, T defaultValue, User user = null);

        /// <summary>
        /// Returns a collection with all the keys.
        /// </summary>
        /// <returns>The key collection.</returns>
        IEnumerable<string> GetAllKeys();

        /// <summary>
        /// Returns a collection with all the keys asynchronously.
        /// </summary>
        /// <returns>The key collection.</returns>
        Task<IEnumerable<string>> GetAllKeysAsync();

        /// <summary>
        /// Refresh the configuration
        /// </summary>
        void ForceRefresh();

        /// <summary>
        /// Refresh the configuration
        /// </summary>
        Task ForceRefreshAsync();

        /// <summary>
        /// Returns the Variation ID (analytics) of a feature flag or setting by the given key.
        /// </summary>
        /// <param name="key">Key for programs</param>
        /// <param name="defaultVariationId">In case of failure return this value</param>
        /// <param name="user">The user object for variation evaluation</param>
        /// <returns>Variation ID</returns>
        string GetVariationId(string key, string defaultVariationId, User user = null);

        /// <summary>
        /// Returns the Variation ID (analytics) of a feature flag or setting by the given key.
        /// </summary>
        /// <param name="key">Key for programs</param>
        /// <param name="defaultVariationId">In case of failure return this value</param>
        /// <param name="user">The user object for variation evaluation</param>
        /// <returns>Variation ID</returns>
        Task<string> GetVariationIdAsync(string key, string defaultVariationId, User user = null);

        /// <summary>
        /// Returns Variation IDs (analytics) of all feature flags or settings.
        /// </summary>
        /// <param name="user">The user object for variation evaluation</param>
        /// <returns>Collection of all Variation IDs</returns>
        IEnumerable<string> GetAllVariationId(User user = null);

        /// <summary>
        /// Returns Variation IDs (analytics) of all feature flags or settings.
        /// </summary>
        /// <param name="user">The user object for variation evaluation</param>
        /// <returns>Collection of all Variation IDs</returns>
        Task<IEnumerable<string>> GetAllVariationIdAsync(User user = null);
    }
}