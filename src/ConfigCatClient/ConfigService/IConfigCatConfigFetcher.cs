using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client;

/// <summary>
/// Defines the interface used by the ConfigCat SDK to perform ConfigCat config fetch operations.
/// </summary>
public interface IConfigCatConfigFetcher : IDisposable
{
    /// <summary>
    /// Fetches the JSON content of the requested config asynchronously.
    /// </summary>
    /// <param name="request">The fetch request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
    /// If the token is canceled, the request should be aborted.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the fetch response.</returns>
    /// <exception cref="FetchErrorException">The fetch operation failed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled during the execution of the task.</exception>
    Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken);
}
