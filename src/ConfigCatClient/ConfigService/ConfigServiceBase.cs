using System;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Utils;

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
        private readonly object syncObj = new();

        protected readonly IConfigFetcher ConfigFetcher;

#pragma warning disable CS0618 // Type or member is obsolete
        protected readonly IConfigCache ConfigCache; // Backward compatibility, it'll be changed to IConfigCatCache later.
#pragma warning restore CS0618 // Type or member is obsolete

        protected readonly LoggerWrapper Log;
        protected readonly string CacheKey;
        protected readonly Hooks Hooks;

        protected ConfigServiceBase(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper log, bool isOffline, Hooks hooks)
        {
            this.ConfigFetcher = configFetcher;
            this.ConfigCache = cacheParameters.ConfigCache;
            this.CacheKey = cacheParameters.CacheKey;
            this.Log = log;
            this.Hooks = hooks ?? NullHooks.Instance;
            this.status = isOffline ? Status.Offline : Status.Online;
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

        public virtual RefreshResult RefreshConfig()
        {
            // check for the new cache interface until we remove the old IConfigCache.
            if (this.ConfigCache is IConfigCatCache cache)
            {
                if (!IsOffline)
                {
                    var latestConfig = cache.Get(this.CacheKey);
                    RefreshConfigCore(latestConfig);
                    return RefreshResult.Success();
                }
                else
                {
                    this.Log.OfflineModeWarning();
                    return RefreshResult.Failure("Client is in offline mode, it can't initiate HTTP calls.");
                }
            }

            // worst scenario, fallback to sync over async, delete when we enforce IConfigCatCache.
            return Syncer.Sync(this.RefreshConfigAsync);
        }

        protected ProjectConfig RefreshConfigCore(ProjectConfig latestConfig)
        {
            var newConfig = this.ConfigFetcher.Fetch(latestConfig);

            var configContentHasChanged = !latestConfig.Equals(newConfig);
            if ((configContentHasChanged || newConfig.TimeStamp > latestConfig.TimeStamp) && !newConfig.Equals(ProjectConfig.Empty))
            {
                // TODO: This cast can be removed when we delete the obsolete IConfigCache interface.
                ((IConfigCatCache)this.ConfigCache).Set(this.CacheKey, newConfig);

                OnConfigUpdated(newConfig);

                if (configContentHasChanged)
                {
                    OnConfigChanged(newConfig);
                }

                return newConfig;
            }

            return latestConfig;
        }

        public virtual async Task<RefreshResult> RefreshConfigAsync()
        {
            if (!IsOffline)
            {
                var latestConfig = await this.ConfigCache.GetAsync(this.CacheKey).ConfigureAwait(false);
                await RefreshConfigCoreAsync(latestConfig).ConfigureAwait(false);
                return RefreshResult.Success();
            }
            else
            {
                this.Log.OfflineModeWarning();
                return RefreshResult.Failure("Client is in offline mode, it can't initiate HTTP calls.");
            }
        }

        protected async Task<ProjectConfig> RefreshConfigCoreAsync(ProjectConfig latestConfig)
        {
            var newConfig = await this.ConfigFetcher.FetchAsync(latestConfig).ConfigureAwait(false);

            var configContentHasChanged = !latestConfig.Equals(newConfig);
            if ((configContentHasChanged || newConfig.TimeStamp > latestConfig.TimeStamp) && !newConfig.Equals(ProjectConfig.Empty))
            {
                await this.ConfigCache.SetAsync(this.CacheKey, newConfig).ConfigureAwait(false);

                OnConfigUpdated(newConfig);

                if (configContentHasChanged)
                {
                    OnConfigChanged(newConfig);
                }

                return newConfig;
            }

            return latestConfig;
        }

        protected virtual void OnConfigUpdated(ProjectConfig newConfig) { }

        protected virtual void OnConfigChanged(ProjectConfig newConfig)
        {
            this.Log.Debug("config changed");
            
            this.Hooks.RaiseConfigChanged(newConfig);
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