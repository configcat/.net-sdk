using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Shims;

internal sealed class DefaultTaskShim : TaskShim
{
    public override Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken = default)
        => Task.Run(function, cancellationToken);

    public override Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        => Task.Delay(delay, cancellationToken);
}
