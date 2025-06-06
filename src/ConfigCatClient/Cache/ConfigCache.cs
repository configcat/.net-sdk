using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Cache;

internal abstract class ConfigCache
{
    protected ConfigCache() { }

    public abstract ProjectConfig LocalCachedConfig { get; }

    public abstract void Set(string key, ProjectConfig config);
    public abstract ValueTask SetAsync(string key, ProjectConfig config, CancellationToken cancellationToken = default);
    public abstract CacheSyncResult Get(string key);
    public abstract ValueTask<CacheSyncResult> GetAsync(string key, CancellationToken cancellationToken = default);
}
