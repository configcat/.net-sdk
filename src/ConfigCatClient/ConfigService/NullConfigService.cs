using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.ConfigService;

internal sealed class NullConfigService : IConfigService
{
    private readonly LoggerWrapper logger;

    public NullConfigService(LoggerWrapper logger, SafeHooksWrapper hooks = default)
    {
        this.logger = logger;

        hooks.RaiseClientReady(ClientCacheState.HasLocalOverrideFlagDataOnly);
    }

    public Task<ClientCacheState> ReadyTask => Task.FromResult(ClientCacheState.HasLocalOverrideFlagDataOnly);

    public ProjectConfig GetInMemoryConfig() => ProjectConfig.Empty;

    public ProjectConfig GetConfig() => ProjectConfig.Empty;

    public ValueTask<ProjectConfig> GetConfigAsync(CancellationToken cancellationToken = default) => new ValueTask<ProjectConfig>(ProjectConfig.Empty);

    public RefreshResult RefreshConfig()
    {
        return RefreshResult.Failure(RefreshErrorCode.LocalOnlyClient,
            $"Client is configured to use the {nameof(OverrideBehaviour.LocalOnly)} override behavior, which prevents synchronization with external cache and making HTTP requests.");
    }

    public ValueTask<RefreshResult> RefreshConfigAsync(CancellationToken cancellationToken = default) => new ValueTask<RefreshResult>(RefreshConfig());

    public bool IsOffline => true;

    public void SetOffline() { /* do nothing */ }

    public void SetOnline()
    {
        this.logger.ConfigServiceMethodHasNoEffectDueToOverrideBehavior(nameof(OverrideBehaviour.LocalOnly), nameof(SetOnline));
    }

    public ClientCacheState GetCacheState(ProjectConfig config) => ClientCacheState.HasLocalOverrideFlagDataOnly;
}
