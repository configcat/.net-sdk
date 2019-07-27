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
    }
}