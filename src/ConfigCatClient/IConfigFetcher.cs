using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client;

/// <summary>
/// Defines the interface of the service used to fetch configs.
/// </summary>
internal interface IConfigFetcher
{
    /// <summary>
    /// Fetches the configuration asynchronously.
    /// </summary>
    /// <param name="lastConfig">Last fetched configuration if it is present.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>>A task that represents the asynchronous operation. The task result contains the fetched config.</returns>
    Task<FetchResult> FetchAsync(ProjectConfig lastConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the configuration synchronously.
    /// </summary>
    /// <param name="lastConfig">Last fetched configuration if it is present.</param>
    /// <returns>The fetched config.</returns>
    FetchResult Fetch(ProjectConfig lastConfig);
}
