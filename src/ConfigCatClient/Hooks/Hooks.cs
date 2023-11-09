using System;
using System.Threading;

namespace ConfigCat.Client;

internal class Hooks : IProvidesHooks
{
    private static readonly EventHandlers DisconnectedEventHandlers = new();

    private volatile EventHandlers eventHandlers;
    private IConfigCatClient? client; // should be null only in case of testing

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

    public virtual void SetSender(IConfigCatClient client)
    {
        this.client = client;
    }

    /// <inheritdoc/>
    public event EventHandler? ClientReady
    {
        add { this.eventHandlers.ClientReady += value; }
        remove { this.eventHandlers.ClientReady -= value; }
    }

    internal void RaiseClientReady()
    {
        this.eventHandlers.ClientReady?.Invoke(this.client, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public event EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated
    {
        add { this.eventHandlers.FlagEvaluated += value; }
        remove { this.eventHandlers.FlagEvaluated -= value; }
    }

    internal void RaiseFlagEvaluated(EvaluationDetails evaluationDetails)
    {
        this.eventHandlers.FlagEvaluated?.Invoke(this.client, new FlagEvaluatedEventArgs(evaluationDetails));
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged
    {
        add { this.eventHandlers.ConfigChanged += value; }
        remove { this.eventHandlers.ConfigChanged -= value; }
    }

    internal void RaiseConfigChanged(IConfig newConfig)
    {
        this.eventHandlers.ConfigChanged?.Invoke(this.client, new ConfigChangedEventArgs(newConfig));
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigCatClientErrorEventArgs>? Error
    {
        add { this.eventHandlers.Error += value; }
        remove { this.eventHandlers.Error -= value; }
    }

    internal void RaiseError(string message, Exception? exception)
    {
        this.eventHandlers.Error?.Invoke(this.client, new ConfigCatClientErrorEventArgs(message, exception));
    }

    protected class EventHandlers
    {
        private static void Noop(Delegate? _) { /* This method is for keeping SonarQube happy. */ }

        public virtual EventHandler? ClientReady { get => null; set => Noop(value); }
        public virtual EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated { get => null; set => Noop(value); }
        public virtual EventHandler<ConfigChangedEventArgs>? ConfigChanged { get => null; set => Noop(value); }
        public virtual EventHandler<ConfigCatClientErrorEventArgs>? Error { get => null; set => Noop(value); }
    }

    private sealed class ActualEventHandlers : EventHandlers
    {
        public override EventHandler? ClientReady { get; set; }
        public override EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated { get; set; }
        public override EventHandler<ConfigChangedEventArgs>? ConfigChanged { get; set; }
        public override EventHandler<ConfigCatClientErrorEventArgs>? Error { get; set; }
    }
}
