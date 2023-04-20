using System.Threading.Tasks;
using System.Threading;

namespace ConfigCat.Client;

/// <summary>
/// Defines the interface used by the ConfigCat SDK to store and retrieve downloaded config JSON data.
/// </summary>
public interface IConfigCatCache
{
    /// <summary>
    /// Stores a value into the cache.
    /// </summary>
    /// <param name="key">A string identifying the value.</param>
    /// <param name="value">The value to cache.</param>
    void Set(string key, string value);

    /// <summary>
    /// Stores a value into the cache.
    /// </summary>
    /// <param name="key">A string identifying the value.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a value from the cache.
    /// </summary>
    /// <param name="key">A string identifying the value.</param>
    /// <returns>The cached value or <see langword="null"/> if there is none.</returns>
    string? Get(string key);

    /// <summary>
    /// Retrieves a value from the cache.
    /// </summary>
    /// <param name="key">A string identifying the value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The cached value or <see langword="null"/> if there is none.</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
}
