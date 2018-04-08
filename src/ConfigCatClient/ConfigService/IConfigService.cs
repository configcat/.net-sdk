using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService
{
    internal interface IConfigService
    {
        Task<ProjectConfig> GetConfigAsync();

        Task RefreshConfigAsync();
    }
}