using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client;

internal sealed class InMemoryConfigCache : IConfigCatCache
{
    private ProjectConfig projectConfig = ProjectConfig.Empty;

    /// <inheritdoc />
    public Task SetAsync(string key, ProjectConfig config)
    {
        Set(key, config);
#if NET45
        return Task.FromResult(0);
#else
        return Task.CompletedTask;
#endif
    }

    /// <inheritdoc />
    public Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Get(key));

    /// <inheritdoc />
    public void Set(string key, ProjectConfig config)
    {
        Interlocked.Exchange(ref this.projectConfig, config);
    }

    /// <inheritdoc />
    public ProjectConfig Get(string key)
    {
        // NOTE: Volatile.Read(ref this.projectConfig) would probably be sufficient but Interlocked.CompareExchange is the 100% safe way.
        return Interlocked.CompareExchange(ref this.projectConfig, null, null);
    }
}
