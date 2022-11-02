using System;

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

        /// <inheritdoc/>
        public virtual event EventHandler ClientReady
        {
            add { this.clientReady += value; }
            remove { this.clientReady -= value; }
        }

        internal void RaiseClientReady(IConfigCatClient client)
        {
            this.clientReady?.Invoke(client, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public virtual event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated
        {
            add { this.flagEvaluated += value; }
            remove { this.flagEvaluated -= value; }
        }

        internal void RaiseFlagEvaluated(IConfigCatClient client, EvaluationDetails evaluationDetails)
        {
            this.flagEvaluated?.Invoke(client, new FlagEvaluatedEventArgs(evaluationDetails));
        }

        /// <inheritdoc/>
        public virtual event EventHandler<ConfigChangedEventArgs> ConfigChanged
        {
            add { this.configChanged += value; }
            remove { this.configChanged -= value; }
        }

        internal void RaiseConfigChanged(IConfigCatClient client, ProjectConfig newConfig)
        {
            this.configChanged?.Invoke(client, new ConfigChangedEventArgs(newConfig));
        }

        /// <inheritdoc/>
        public virtual event EventHandler<ConfigCatClientErrorEventArgs> Error
        {
            add { this.error += value; }
            remove { this.error -= value; }
        }

        internal void RaiseError(IConfigCatClient client, string message, Exception exception)
        {
            this.error?.Invoke(client, new ConfigCatClientErrorEventArgs(message, exception));
        }

        /// <inheritdoc/>
        public virtual event EventHandler BeforeClientDispose
        {
            add { this.beforeClientDispose += value; }
            remove { this.beforeClientDispose -= value; }
        }

        internal void RaiseBeforeClientDispose(IConfigCatClient client)
        {
            this.beforeClientDispose?.Invoke(client, EventArgs.Empty);
        }
    }
}
