using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService;

internal sealed class NullConfigService : IConfigService
{
    private readonly LoggerWrapper logger;

    public NullConfigService(LoggerWrapper logger, Hooks? hooks = null)
    {
        this.logger = logger;

        hooks?.RaiseClientReady();
    }

    public ProjectConfig GetConfig() => ProjectConfig.Empty;

    public Task<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default) => Task.FromResult(ProjectConfig.Empty);

    public RefreshResult RefreshConfig() { return RefreshResult.Failure($"Client is configured to use the {nameof(OverrideBehaviour.LocalOnly)} override behavior, which prevents making HTTP requests."); }

    public Task<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default) => Task.FromResult(RefreshConfig());

    public bool IsOffline => true;

    public void SetOffline() { /* do nothing */ }

    public void SetOnline()
    {
        this.logger.ConfigServiceMethodHasNoEffectDueToOverrideBehavior(nameof(OverrideBehaviour.LocalOnly), nameof(SetOnline));
    }
}
