using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Cache;

internal sealed class InMemoryConfigCache : ConfigCache
{
    private ProjectConfig cachedConfig = ProjectConfig.Empty;

    public override ProjectConfig Get(string key)
    {
        return Volatile.Read(ref this.cachedConfig);
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
        // Read the current cached config.
        var currentCachedConfig = Volatile.Read(ref this.cachedConfig);
        for (; ; )
        {
            // If the specified config is not newer than what we have in the cache currently, ignore it.
            if (!config.IsNewerThan(currentCachedConfig))
            {
                return;
            }

            // Otherwise, let's try to overwrite the current config with the specified one.
            var originalValue = Interlocked.CompareExchange(ref this.cachedConfig, config, currentCachedConfig);

            // Overwrite succeeded?
            if (originalValue == currentCachedConfig)
            {
                return;
            }

            // Another thread has changed the cached config in the meantime. Rinse & repeat.
            currentCachedConfig = originalValue;
        }
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
