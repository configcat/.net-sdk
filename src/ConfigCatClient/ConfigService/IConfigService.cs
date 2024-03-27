using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService;

internal interface IConfigService
{
    Task<ClientCacheState> ReadyTask { get; }

    ProjectConfig GetInMemoryConfig();

    ProjectConfig GetConfig();

    ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default);

    RefreshResult RefreshConfig();

    ValueTask<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default);

    bool IsOffline { get; }

    void SetOnline();

    void SetOffline();

    ClientCacheState GetCacheState(ProjectConfig config);
}
