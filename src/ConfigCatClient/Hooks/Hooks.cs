using System;
using System.Runtime.CompilerServices;

namespace ConfigCat.Client
{
    internal class Hooks : IProvidesHooks
    {
        // NOTE: SonarQube doesn't like virtual field-like events, so we need to define them with the explicit add/remove pattern.

        private EventHandler clientReady;
        private EventHandler<FlagEvaluatedEventArgs> flagEvaluated;
        private EventHandler<ConfigChangedEventArgs> configChanged;
        private EventHandler<ConfigCatClientErrorEventArgs> error;
        private EventHandler beforeClientDispose;

        private WeakReference<IConfigCatClient> clientWeakRef; // should be null only in case of testing

        private bool TryGetSender(out IConfigCatClient client)
        {
            if (this.clientWeakRef == null)
            {
                client = null;
                return true;
            }

            return this.clientWeakRef.TryGetTarget(out client);
        }

        public void SetSender(IConfigCatClient client)
        {
            // Strong back-references to the client instance must be avoided so GC can collect it when user doesn't have references to it any more.
            // (There are cases - like AutoPollConfigService or LocalFileDataSource - where the background work keeps the service object alive,
            // so if that had a strong reference to the client object, it would be kept alive as well, which would create a memory leak.)

            this.clientWeakRef = new WeakReference<IConfigCatClient>(client);
        }

        /// <inheritdoc/>
        public virtual event EventHandler ClientReady
        {
            add { this.clientReady += value; }
            remove { this.clientReady -= value; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void RaiseClientReady()
        {
            if (this.clientReady is { } clientReady && TryGetSender(out var client))
            {
                clientReady(client, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public virtual event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated
        {
            add { this.flagEvaluated += value; }
            remove { this.flagEvaluated -= value; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void RaiseFlagEvaluated(EvaluationDetails evaluationDetails)
        {
            if (this.flagEvaluated is { } flagEvaluated && TryGetSender(out var client))
            {
                flagEvaluated(client, new FlagEvaluatedEventArgs(evaluationDetails));
            }
        }

        /// <inheritdoc/>
        public virtual event EventHandler<ConfigChangedEventArgs> ConfigChanged
        {
            add { this.configChanged += value; }
            remove { this.configChanged -= value; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void RaiseConfigChanged(ProjectConfig newConfig)
        {
            if (this.configChanged is { } configChanged && TryGetSender(out var client))
            {
                configChanged(client, new ConfigChangedEventArgs(newConfig));
            }
        }

        /// <inheritdoc/>
        public virtual event EventHandler<ConfigCatClientErrorEventArgs> Error
        {
            add { this.error += value; }
            remove { this.error -= value; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void RaiseError(string message, Exception exception)
        {
            if (this.error is { } error && TryGetSender(out var client))
            {
                error(client, new ConfigCatClientErrorEventArgs(message, exception));
            }
        }

        /// <inheritdoc/>
        public virtual event EventHandler BeforeClientDispose
        {
            add { this.beforeClientDispose += value; }
            remove { this.beforeClientDispose -= value; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void RaiseBeforeClientDispose()
        {
            if (this.beforeClientDispose is { } beforeClientDispose && TryGetSender(out var client))
            {
                beforeClientDispose(client, EventArgs.Empty);
            }
        }
    }
}
