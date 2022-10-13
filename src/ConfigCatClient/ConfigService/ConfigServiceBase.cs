using System;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.ConfigService
{
    internal abstract class ConfigServiceBase : IDisposable
    {
        protected internal enum Status
        {
            Online,
            Offline,
            Disposed,
        }
 
        private Status status;
        private readonly object syncObj = new object();

        protected readonly IConfigFetcher ConfigFetcher;

#pragma warning disable CS0618 // Type or member is obsolete
        protected readonly IConfigCache ConfigCache; // Backward compatibility, it'll be changed to IConfigCatCache later.
#pragma warning restore CS0618 // Type or member is obsolete

        protected readonly LoggerWrapper Log;

        protected readonly string CacheKey;

        protected ConfigServiceBase(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper log)
        {
            this.ConfigFetcher = configFetcher;
            this.ConfigCache = cacheParameters.ConfigCache;
            this.CacheKey = cacheParameters.CacheKey;
            this.Log = log;
        }

        /// <remarks>
        /// Note for inheritors. Beware, this method is called within a lock statement.
        /// </remarks>
        protected virtual void DisposeSynchronized(bool disposing) { }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && ConfigFetcher is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public void Dispose()
        {
            lock (this.syncObj)
            {
                if (this.status == Status.Disposed)
                {
                    return;
                }

                this.status = Status.Disposed;

                DisposeSynchronized(true);
            }

            Dispose(true);
        }

        public bool IsOffline
        {
            get
            {
                lock (this.syncObj)
                {
                    return this.status != Status.Online;
                }
            }
        }

        /// <remarks>
        /// Note for inheritors. Beware, this method is called within a lock statement.
        /// </remarks>
        protected virtual void SetOnlineCoreSynchronized() { }

        public void SetOnline()
        {
            Action<ILogger> logAction = null;

            lock (this.syncObj)
            {
                if (this.status == Status.Offline)
                {
                    SetOnlineCoreSynchronized();
                    this.status = Status.Online;
                    logAction = static logger => logger.StatusChange(Status.Online);
                }
                else if (this.status == Status.Disposed)
                {
                    logAction = static logger => logger.DisposedWarning(nameof(SetOnline) + "()");
                    return;
                }
            }

            logAction?.Invoke(this.Log);
        }

        /// <remarks>
        /// Note for inheritors. Beware, this method is called within a lock statement.
        /// </remarks>
        protected virtual void SetOfflineCoreSynchronized() { }

        public void SetOffline()
        {
            Action<ILogger> logAction = null;

            lock (this.syncObj)
            {
                if (this.status == Status.Online)
                {
                    SetOfflineCoreSynchronized();
                    this.status = Status.Offline;
                    logAction = static logger => logger.StatusChange(Status.Offline);
                }
                else if (this.status == Status.Disposed)
                {
                    logAction = static logger => logger.DisposedWarning(nameof(SetOffline) + "()");
                }
            }

            logAction?.Invoke(this.Log);
        }

        protected TResult Synchronize<TState, TResult>(Func<TState, TResult> func, TState state)
        {
            lock (this.syncObj)
            {
                return func(state);
            }
        }
    }
}