using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Cache;

internal sealed class InMemoryConfigCache : ConfigCache
{
    private volatile ProjectConfig cachedConfig = ProjectConfig.Empty;

    public override ProjectConfig LocalCachedConfig => this.cachedConfig;

    public override ProjectConfig Get(string key)
    {
        return LocalCachedConfig;
    }

    public override ValueTask<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<ProjectConfig>(cancellationToken.ToTask<ProjectConfig>());
        }

        return new ValueTask<ProjectConfig>(Get(key));
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
