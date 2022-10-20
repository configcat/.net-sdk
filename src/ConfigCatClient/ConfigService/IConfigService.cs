using System;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService
{
    internal interface IConfigService
    {
        ProjectConfig GetConfig();

        Task<ProjectConfig> GetConfigAsync();

        Task RefreshConfigAsync();

        void RefreshConfig();

        bool IsOffline { get; }

        void SetOnline();

        void SetOffline();
    }
}