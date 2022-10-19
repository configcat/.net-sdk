using System;
using System.Runtime.CompilerServices;

namespace ConfigCat.Client
{
    internal sealed class ConfigCatClientContext
    {
        public static readonly ConfigCatClientContext None = new(static _ => NullHooks.Instance);

        private readonly WeakReference<IConfigCatClient> clientWeakRef; // allowed to be null only in case of testing
        private readonly Func<IConfigCatClient, Hooks> hooksAccessor; // never allowed to be null

        // For testing purposes only.
        internal ConfigCatClientContext(Func<IConfigCatClient, Hooks> hooksAccessor)
        {
            this.hooksAccessor = hooksAccessor;
        }

        public ConfigCatClientContext(WeakReference<IConfigCatClient> clientWeakRef, Func<IConfigCatClient, Hooks> hooksAccessor) : this(hooksAccessor)
        {
            this.clientWeakRef = clientWeakRef; 
        }

        public bool ClientIsGone
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => this.clientWeakRef is not null && !this.clientWeakRef.TryGetTarget(out _);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RaiseClientReady()
        {
            IConfigCatClient client = null;
            if (this.clientWeakRef is null || this.clientWeakRef.TryGetTarget(out client))
            {
                hooksAccessor(client).RaiseClientReady(client);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RaiseConfigChanged(ProjectConfig newConfig)
        {
            IConfigCatClient client = null;
            if (this.clientWeakRef is null || this.clientWeakRef.TryGetTarget(out client))
            {
                hooksAccessor(client).RaiseConfigChanged(client, newConfig);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RaiseError(string message, Exception exception)
        {
            IConfigCatClient client = null;
            if (this.clientWeakRef is null || this.clientWeakRef.TryGetTarget(out client))
            {
                hooksAccessor(client).RaiseError(client, message, exception);
            }
        }
    }
}
