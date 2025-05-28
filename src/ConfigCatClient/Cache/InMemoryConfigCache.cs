using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Cache;

internal sealed class InMemoryConfigCache : ConfigCache
{
    private volatile ProjectConfig cachedConfig = ProjectConfig.Empty;

    public override ProjectConfig LocalCachedConfig => this.cachedConfig;

    public override CacheSyncResult Get(string key)
    {
        return new CacheSyncResult(LocalCachedConfig);
    }

    public override ValueTask<CacheSyncResult> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<CacheSyncResult>(cancellationToken.ToTask<CacheSyncResult>());
        }

        return new ValueTask<CacheSyncResult>(Get(key));
    }

    public override void Set(string key, ProjectConfig config)
    {
        this.cachedConfig = config;
    }

    public override ValueTask SetAsync(string key, ProjectConfig config, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask(cancellationToken.ToTask<ProjectConfig>());
        }

        Set(key, config);

        return default;
    }
}
