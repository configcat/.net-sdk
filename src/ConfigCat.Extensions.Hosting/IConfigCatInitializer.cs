using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Extensions.Hosting;

/// <summary>
/// Provides functionality for initializing <see cref="IConfigCatClient"/> services registered in the DI container.
/// </summary>
public interface IConfigCatInitializer
{
    /// <summary>
    /// Initializes all <see cref="IConfigCatClient"/> services registered in the DI container.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled during the execution of the task.</exception>
    /// <exception cref="TimeoutException">One or more clients using Auto Polling mode failed to obtain config data within the configured <see cref="AutoPoll.MaxInitWaitTime"/>.</exception>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
