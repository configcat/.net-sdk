using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ConfigCat.Extensions.Hosting.Tests.Fakes;

internal sealed class FakeHost(IServiceProvider services,
    Func<IServiceProvider, CancellationToken, Task>? startAsync = null,
    Func<IServiceProvider, CancellationToken, Task>? stopAsync = null)
    : IHost
{
    public IServiceProvider Services { get; } = services;

    public void Dispose() => (Services as IDisposable)?.Dispose();

    public Task StartAsync(CancellationToken cancellationToken = default)
        => startAsync is not null ? startAsync(Services, cancellationToken) : Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default)
        => stopAsync is not null ? stopAsync(Services, cancellationToken) : Task.CompletedTask;
}
