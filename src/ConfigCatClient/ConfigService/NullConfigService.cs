using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class NullConfigService : IConfigService
    {
        private readonly LoggerWrapper log;

        public NullConfigService(LoggerWrapper log, Hooks hooks = null)
        {
            this.log = log;

            hooks?.RaiseClientReady();
        }

        public ProjectConfig GetConfig() => ProjectConfig.Empty;

        public Task<ProjectConfig> GetConfigAsync() => Task.FromResult(ProjectConfig.Empty);

        public RefreshResult RefreshConfig() { return RefreshResult.Failure($"Client is configured to use the {nameof(OverrideBehaviour.LocalOnly)} override behavior, which prevents making HTTP requests."); }

        public Task<RefreshResult> RefreshConfigAsync() => Task.FromResult(RefreshConfig());

        public bool IsOffline => true;

        public void SetOffline() { /* do nothing */ }

        public void SetOnline() 
        {
            this.log.Warning($"Client is configured to use the {nameof(OverrideBehaviour.LocalOnly)} override behavior, thus {nameof(SetOnline)}() has no effect.");
        }
    }
}
