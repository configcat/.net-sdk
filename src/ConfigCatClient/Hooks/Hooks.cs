using System;
using System.Threading;

namespace ConfigCat.Client;

internal class Hooks : IProvidesHooks
{
    private static readonly Events DisconnectedEvents = new();

    private volatile Events events;
    private IConfigCatClient? client; // should be null only in case of testing

    protected Hooks(Events events)
    {
        this.events = events;
    }

    public Hooks() : this(new RealEvents()) { }

    public virtual bool TryDisconnect()
    {
        // Replacing the current Events object (this.events) with a special instance of Events (DisconnectedEvents) achieves multiple things:
        // 1. determines whether the hooks instance has already been disconnected or not,
        // 2. removes implicit references to subscriber objects (so this instance won't keep them alive under any circumstances),
        // 3. makes sure that future subscriptions are ignored from this point on.
        var originalEvents = Interlocked.Exchange(ref this.events, DisconnectedEvents);

        return !ReferenceEquals(originalEvents, DisconnectedEvents);
    }

    public virtual void SetSender(IConfigCatClient client)
    {
        this.client = client;
    }

    public void RaiseClientReady()
        => this.events.RaiseClientReady(this.client);

    public void RaiseFlagEvaluated(EvaluationDetails evaluationDetails)
        => this.events.RaiseFlagEvaluated(this.client, evaluationDetails);

    public void RaiseConfigFetched(RefreshResult result, bool isInitiatedByUser)
        => this.events.RaiseConfigFetched(this.client, result, isInitiatedByUser);

    public void RaiseConfigChanged(IConfig newConfig)
        => this.events.RaiseConfigChanged(this.client, newConfig);

    public void RaiseError(string message, Exception? exception)
        => this.events.RaiseError(this.client, message, exception);

    public event EventHandler? ClientReady
    {
        add { this.events.ClientReady += value; }
        remove { this.events.ClientReady -= value; }
    }

    public event EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated
    {
        add { this.events.FlagEvaluated += value; }
        remove { this.events.FlagEvaluated -= value; }
    }

    public event EventHandler<ConfigFetchedEventArgs>? ConfigFetched
    {
        add { this.events.ConfigFetched += value; }
        remove { this.events.ConfigFetched -= value; }
    }

    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged
    {
        add { this.events.ConfigChanged += value; }
        remove { this.events.ConfigChanged -= value; }
    }

    public event EventHandler<ConfigCatClientErrorEventArgs>? Error
    {
        add { this.events.Error += value; }
        remove { this.events.Error -= value; }
    }

    public class Events : IProvidesHooks
    {
        public virtual void RaiseClientReady(IConfigCatClient? client) { /* intentional no-op */ }
        public virtual void RaiseFlagEvaluated(IConfigCatClient? client, EvaluationDetails evaluationDetails) { /* intentional no-op */ }
        public virtual void RaiseConfigFetched(IConfigCatClient? client, RefreshResult result, bool isInitiatedByUser) { /* intentional no-op */ }
        public virtual void RaiseConfigChanged(IConfigCatClient? client, IConfig newConfig) { /* intentional no-op */ }
        public virtual void RaiseError(IConfigCatClient? client, string message, Exception? exception) { /* intentional no-op */ }

        public virtual event EventHandler? ClientReady { add { /* intentional no-op */ } remove { /* intentional no-op */ } }
        public virtual event EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated { add { /* intentional no-op */ } remove { /* intentional no-op */ } }
        public virtual event EventHandler<ConfigFetchedEventArgs>? ConfigFetched { add { /* intentional no-op */ } remove { /* intentional no-op */ } }
        public virtual event EventHandler<ConfigChangedEventArgs>? ConfigChanged { add { /* intentional no-op */ } remove { /* intentional no-op */ } }
        public virtual event EventHandler<ConfigCatClientErrorEventArgs>? Error { add { /* intentional no-op */ } remove { /* intentional no-op */ } }
    }

    private sealed class RealEvents : Events
    {
        public override void RaiseClientReady(IConfigCatClient? client)
        {
            ClientReady?.Invoke(client, EventArgs.Empty);
        }

        public override void RaiseFlagEvaluated(IConfigCatClient? client, EvaluationDetails evaluationDetails)
        {
            FlagEvaluated?.Invoke(client, new FlagEvaluatedEventArgs(evaluationDetails));
        }

        public override void RaiseConfigFetched(IConfigCatClient? client, RefreshResult result, bool isInitiatedByUser)
        {
            ConfigFetched?.Invoke(client, new ConfigFetchedEventArgs(result, isInitiatedByUser));
        }

        public override void RaiseConfigChanged(IConfigCatClient? client, IConfig newConfig)
        {
            ConfigChanged?.Invoke(client, new ConfigChangedEventArgs(newConfig));
        }

        public override void RaiseError(IConfigCatClient? client, string message, Exception? exception)
        {
            Error?.Invoke(client, new ConfigCatClientErrorEventArgs(message, exception));
        }

        public override event EventHandler? ClientReady;
        public override event EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated;
        public override event EventHandler<ConfigFetchedEventArgs>? ConfigFetched;
        public override event EventHandler<ConfigChangedEventArgs>? ConfigChanged;
        public override event EventHandler<ConfigCatClientErrorEventArgs>? Error;
    }
}
