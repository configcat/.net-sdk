using System;
using System.Runtime.CompilerServices;

namespace ConfigCat.Client;

// Strong back-references to the client instance must be avoided so GC can collect it when user doesn't have references to it any more.
// E.g. if a strong reference chain like AutoPollConfigService -> ... -> ConfigCatClient existed, the client instance could not be collected
// because the background polling loop would keep the AutoPollConfigService alive indefinetely, which in turn would keep alive ConfigCatClient.
// We need to break such strong reference chains with a weak reference somewhere. As consumers are free to add hook event handlers which
// close over the client instance (e.g. client.ConfigChanged += (_, e) => { client.GetValue(...) }), that is, a chain like
// AutoPollConfigService -> Hooks -> EventHandler.Target -> ConfigCatClient can be created, it is the hooks reference that we need to make weak.
internal readonly struct SafeHooksWrapper
{
    private readonly WeakReference<Hooks> hooksWeakRef;

    private Hooks Hooks => this.hooksWeakRef is { } hooksWeakRef && hooksWeakRef.TryGetTarget(out var hooks) ? hooks! : NullHooks.Instance;

    public SafeHooksWrapper(Hooks hooks)
    {
        this.hooksWeakRef = new WeakReference<Hooks>(hooks);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RaiseClientReady(ClientCacheState cacheState) => Hooks.RaiseClientReady(cacheState);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RaiseFlagEvaluated(EvaluationDetails evaluationDetails) => Hooks.RaiseFlagEvaluated(evaluationDetails);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RaiseConfigChanged(IConfig newConfig) => Hooks.RaiseConfigChanged(newConfig);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RaiseError(string message, Exception? exception) => Hooks.RaiseError(message, exception);

    public static implicit operator SafeHooksWrapper(Hooks? hooks) => hooks is not null ? new SafeHooksWrapper(hooks) : default;
}
