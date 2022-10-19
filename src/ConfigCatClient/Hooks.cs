using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Provides data for the <see cref="ConfigCatClient.FlagEvaluated"/> event.
    /// </summary>
    public class FlagEvaluatedEventArgs : EventArgs
    {
        internal FlagEvaluatedEventArgs(EvaluationDetails evaluationDetails)
        {
            EvaluationDetails = evaluationDetails;
        }

        /// <summary>
        /// The <see cref="Client.EvaluationDetails"/> object resulted from the evaluation of a feature or setting flag.
        /// </summary>
        public EvaluationDetails EvaluationDetails { get; }
    }

    /// <summary>
    /// Provides data for the <see cref="ConfigCatClient.ConfigChanged"/> event.
    /// </summary>
    public class ConfigChangedEventArgs : EventArgs
    {
        internal ConfigChangedEventArgs(ProjectConfig newConfig)
        {
            NewConfig = newConfig;
        }

        /// <summary>
        /// The new <see cref="ProjectConfig"/> object.
        /// </summary>
        public ProjectConfig NewConfig { get; }
    }

    /// <summary>
    /// Provides data for the <see cref="ConfigCatClient.Error"/> event.
    /// </summary>
    public class ConfigCatClientErrorEventArgs : EventArgs
    {
        internal ConfigCatClientErrorEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The <see cref="System.Exception"/> object related to the error (if any).
        /// </summary>
        public Exception Exception { get; }
    }

    /// <summary>
    /// Defines hooks (events) for providing notifications of <see cref="ConfigCatClient"/>'s actions.
    /// </summary>
    public interface IProvidesHooks
    {
        /// <summary>
        /// Occurs when the client is ready to provide the actual value of feature flags or settings.
        /// </summary>
        event EventHandler ClientReady;

        /// <summary>
        /// Occurs after the value of a feature flag of setting has been evaluated.
        /// </summary>
        event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated;

        /// <summary>
        /// Occurs after the configuration has been updated.
        /// </summary>
        event EventHandler<ConfigChangedEventArgs> ConfigChanged;

        /// <summary>
        /// Occurs in the case of a failure in the client.
        /// </summary>
        event EventHandler<ConfigCatClientErrorEventArgs> Error;

        /// <summary>
        /// Occurs before the client is closed by <see cref="IDisposable.Dispose"/>.
        /// </summary>
        event EventHandler BeforeClientDispose;
    }

    internal class Hooks : IProvidesHooks
    {
        // NOTE: Sonar doesn't like virtual field-like events, so we need to define them with the explicit add/remove pattern.

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

    internal sealed class NullHooks : Hooks
    {
        private static void Noop(Delegate eventHandler) { /* This method is for keeping Sonar happy. */ }

        public static readonly NullHooks Instance = new();

        public override event EventHandler ClientReady
        { 
            add { Noop(value); } 
            remove { Noop(value); }
        }

        public override event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated
        {
            add { Noop(value); }
            remove { Noop(value); }
        }

        public override event EventHandler<ConfigChangedEventArgs> ConfigChanged
        {
            add { Noop(value); }
            remove { Noop(value); }
        }

        public override event EventHandler<ConfigCatClientErrorEventArgs> Error
        {
            add { Noop(value); }
            remove { Noop(value); }
        }

        public override event EventHandler BeforeClientDispose
        {
            add { Noop(value); }
            remove { Noop(value); }
        }
    }
}
