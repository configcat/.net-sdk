using System;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService;

internal interface IConfigService
{
    ProjectConfig GetConfig();

    Task<ProjectConfig> GetConfigAsync();

    Task<RefreshResult> RefreshConfigAsync();

    RefreshResult RefreshConfig();

    bool IsOffline { get; }

    void SetOnline();

    void SetOffline();
}
