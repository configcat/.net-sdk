using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client;

/// <summary>
/// Defines the interface used by the ConfigCat SDK to store and retrieve downloaded config data.
/// </summary>
public interface IConfigCatCache
{
    /// <summary>
    /// Stores a data item into the cache asynchronously.
    /// </summary>
    /// <param name="key">A string identifying the data item.</param>
    /// <param name="value">The data item to cache.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a data item from the cache asynchronously.
    /// </summary>
    /// <param name="key">A string identifying the data item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the cached data item or <see langword="null"/> if there is none.</returns>
    ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default);
}
