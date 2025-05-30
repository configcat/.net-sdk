using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests.Fakes;

internal sealed class FakeExternalCache : IConfigCatCache
{
    public volatile string? CachedValue = null;

    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return new ValueTask<string?>(this.CachedValue);
    }

    public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        this.CachedValue = value;
        return default;
    }
}

internal sealed class FakeExternalAsyncCache : IConfigCatCache
{
    public volatile string? CachedValue = null;

    private readonly TimeSpan delay;

    public FakeExternalAsyncCache(TimeSpan delay)
    {
        this.delay = delay;
    }

    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.Delay(this.delay, cancellationToken);
        return this.CachedValue;
    }

    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await Task.Delay(this.delay, cancellationToken);
        this.CachedValue = value;
    }
}

internal sealed class FaultyFakeExternalCache : IConfigCatCache
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        throw new ApplicationException("Operation failed :(");
    }

    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        throw new ApplicationException("Operation failed :(");
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
