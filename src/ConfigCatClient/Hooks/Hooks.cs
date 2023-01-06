using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ConfigCat.Client;

internal class Hooks : IProvidesHooks
{
    private static readonly EventHandlers DisconnectedEventHandlers = new();

    private volatile EventHandlers eventHandlers;
    private WeakReference<IConfigCatClient> clientWeakRef; // should be null only in case of testing

    protected Hooks(EventHandlers eventHandlers)
    {
        this.eventHandlers = eventHandlers;
    }

    public Hooks() : this(new ActualEventHandlers()) { }

    public virtual bool TryDisconnect()
    {
        // Replacing the current EventHandlers object (eventHandlers) with a special instance of EventHandlers (DisconnectedEventHandlers) achieves multiple things:
        // 1. determines whether the hooks instance has already been disconnected or not,
        // 2. removes implicit references to subscriber objects (so this instance won't keep them alive under any circumstances),
        // 3. makes sure that future subscriptions are ignored from this point on.
        var originalEventHandlers = Interlocked.Exchange(ref this.eventHandlers, DisconnectedEventHandlers);

        return !ReferenceEquals(originalEventHandlers, DisconnectedEventHandlers);
    }

    private bool TryGetSender(out IConfigCatClient client)
    {
        if (this.clientWeakRef == null)
        {
            client = null;
            return true;
        }

        return this.clientWeakRef.TryGetTarget(out client);
    }

    public virtual void SetSender(IConfigCatClient client)
    {
        // Strong back-references to the client instance must be avoided so GC can collect it when user doesn't have references to it any more.
        // (There are cases - like AutoPollConfigService or LocalFileDataSource - where the background work keeps the service object alive,
        // so if that had a strong reference to the client object, it would be kept alive as well, which would create a memory leak.)
        this.clientWeakRef = new WeakReference<IConfigCatClient>(client);
    }

    /// <inheritdoc/>
    public event EventHandler ClientReady
    {
        add { this.eventHandlers.ClientReady += value; }
        remove { this.eventHandlers.ClientReady -= value; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void RaiseClientReady()
    {
        if (this.eventHandlers.ClientReady is { } clientReady && TryGetSender(out var client))
        {
            clientReady(client, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    public event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated
    {
        add { this.eventHandlers.FlagEvaluated += value; }
        remove { this.eventHandlers.FlagEvaluated -= value; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void RaiseFlagEvaluated(EvaluationDetails evaluationDetails)
    {
        if (this.eventHandlers.FlagEvaluated is { } flagEvaluated && TryGetSender(out var client))
        {
            flagEvaluated(client, new FlagEvaluatedEventArgs(evaluationDetails));
        }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigChangedEventArgs> ConfigChanged
    {
        add { this.eventHandlers.ConfigChanged += value; }
        remove { this.eventHandlers.ConfigChanged -= value; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void RaiseConfigChanged(ProjectConfig newConfig)
    {
        if (this.eventHandlers.ConfigChanged is { } configChanged && TryGetSender(out var client))
        {
            configChanged(client, new ConfigChangedEventArgs(newConfig));
        }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigCatClientErrorEventArgs> Error
    {
        add { this.eventHandlers.Error += value; }
        remove { this.eventHandlers.Error -= value; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void RaiseError(string message, Exception exception)
    {
        if (this.eventHandlers.Error is { } error && TryGetSender(out var client))
        {
            error(client, new ConfigCatClientErrorEventArgs(message, exception));
        }
    }

    protected class EventHandlers
    {
        private static void Noop(Delegate _) { /* This method is for keeping SonarQube happy. */ }

        public virtual EventHandler ClientReady { get => null; set => Noop(value); }
        public virtual EventHandler<FlagEvaluatedEventArgs> FlagEvaluated { get => null; set => Noop(value); }
        public virtual EventHandler<ConfigChangedEventArgs> ConfigChanged { get => null; set => Noop(value); }
        public virtual EventHandler<ConfigCatClientErrorEventArgs> Error { get => null; set => Noop(value); }
    }

    private sealed class ActualEventHandlers : EventHandlers
    {
        public override EventHandler ClientReady { get; set; }
        public override EventHandler<FlagEvaluatedEventArgs> FlagEvaluated { get; set; }
        public override EventHandler<ConfigChangedEventArgs> ConfigChanged { get; set; }
        public override EventHandler<ConfigCatClientErrorEventArgs> Error { get; set; }
    }
}
