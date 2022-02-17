using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class NullConfigService : IConfigService
    {
        public ProjectConfig GetConfig() => ProjectConfig.Empty;

        public Task<ProjectConfig> GetConfigAsync() => Task.FromResult(ProjectConfig.Empty);

        public void RefreshConfig() { }

        public Task RefreshConfigAsync() => Task.FromResult(0);
    }
}
