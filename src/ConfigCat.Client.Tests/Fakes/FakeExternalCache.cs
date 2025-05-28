using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests.Fakes;

internal sealed class FakeExternalCache : IConfigCatCache
{
    public volatile string? CachedValue = null;

    public string? Get(string key) => this.CachedValue;

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Get(key));

    public void Set(string key, string value) => this.CachedValue = value;

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        Set(key, value);
        return Task.FromResult(0);
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

    public string? Get(string key)
    {
        Thread.Sleep(this.delay);
        return this.CachedValue;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.Delay(this.delay, cancellationToken);
        return this.CachedValue;
    }

    public void Set(string key, string value)
    {
        Thread.Sleep(this.delay);
        this.CachedValue = value;
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await Task.Delay(this.delay, cancellationToken);
        this.CachedValue = value;
    }
}

internal sealed class FaultyFakeExternalCache : IConfigCatCache
{
    public string? Get(string key) => throw new ApplicationException("Operation failed :(");

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Get(key));

    public void Set(string key, string value) => throw new ApplicationException("Operation failed :(");

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        Set(key, value);
        return Task.FromResult(0);
    }
}
