using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService;

internal interface IConfigService
{
    ProjectConfig GetConfig();

    Task<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default);

    RefreshResult RefreshConfig();

    Task<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default);

    bool IsOffline { get; }

    void SetOnline();

    void SetOffline();
}
