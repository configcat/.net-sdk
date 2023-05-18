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

    public override Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return cancellationToken.ToTask<ProjectConfig>();
        }

        return Task.FromResult(Get(key));
    }

    public override void Set(string key, ProjectConfig config)
    {
        this.cachedConfig = config;
    }

    public override Task SetAsync(string key, ProjectConfig config, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return cancellationToken.ToTask<ProjectConfig>();
        }

        Set(key, config);
#if NET45
        return Task.FromResult(0);
#else
        return Task.CompletedTask;
#endif
    }
}
