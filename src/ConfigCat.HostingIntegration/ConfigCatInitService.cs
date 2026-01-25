using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ConfigCat.HostingIntegration;

internal sealed class ConfigCatInitService(IConfigCatInitializer initializer, ConfigCatInitArgs args) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        var argsLocal = args;
        args = null!; // not needed any more, let GC clean it up
        return initializer.InitializeAsync(argsLocal, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
