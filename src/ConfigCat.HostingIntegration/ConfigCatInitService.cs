using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ConfigCat.HostingIntegration;

internal sealed class ConfigCatInitService(IConfigCatInitializer initializer) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken) => initializer.InitializeAsync(cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
