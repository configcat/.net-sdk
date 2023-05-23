using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService;

internal interface IConfigService
{
    ProjectConfig GetConfig();

    ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default);

    RefreshResult RefreshConfig();

    ValueTask<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default);

    bool IsOffline { get; }

    void SetOnline();

    void SetOffline();
}
