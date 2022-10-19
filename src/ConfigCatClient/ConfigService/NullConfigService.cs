using System;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService
{
    internal sealed class NullConfigService : IConfigService
    {
        private readonly LoggerWrapper log;

        public NullConfigService(LoggerWrapper log, ConfigCatClientContext clientContext = null)
        {
            this.log = log;

            (clientContext ?? ConfigCatClientContext.None).RaiseClientReady();
        }

        public ProjectConfig GetConfig() => ProjectConfig.Empty;

        public Task<ProjectConfig> GetConfigAsync() => Task.FromResult(ProjectConfig.Empty);

        public void RefreshConfig() { /* do nothing */ }

        public Task RefreshConfigAsync() => Task.FromResult(0);

        public bool IsOffline => true;

        public void SetOffline() { /* do nothing */ }

        public void SetOnline() 
        {
            this.log.Warning($"Client is configured to use Local/Offline mode, thus {nameof(SetOnline)}() has no effect.");
        }
    }
}
