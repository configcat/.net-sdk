using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Cache;

internal abstract class ConfigCache
{
    protected ConfigCache() { }

    public abstract void Set(string key, ProjectConfig config);
    public abstract Task SetAsync(string key, ProjectConfig config, CancellationToken cancellationToken = default);
    public abstract ProjectConfig Get(string key);
    public abstract Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default);
}
