using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests.Fakes;

internal sealed class FakeConfigFetcher : IConfigCatConfigFetcher
{
    private readonly Func<FetchRequest, Task<FetchResponse>> fetchCallback;

    public FakeConfigFetcher(Func<FetchRequest, Task<FetchResponse>> fetchCallback)
    {
        this.fetchCallback = fetchCallback;
    }

    public void Dispose() { /* intentional no-op */ }

    public Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken)
    {
        return this.fetchCallback(request);
    }
}
