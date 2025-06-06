using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client.Cache;

internal sealed class ExternalConfigCache : ConfigCache
{
    private readonly object syncObj = new();
    private readonly IConfigCatCache cache;
    private readonly LoggerWrapper logger;
    private ProjectConfig cachedConfig = ProjectConfig.Empty;
    private string? cachedSerializedConfig;

    public ExternalConfigCache(IConfigCatCache cache, LoggerWrapper logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    public override ProjectConfig LocalCachedConfig
    {
        get
        {
            lock (this.syncObj)
            {
                return this.cachedConfig;
            }
        }
    }

    public override async ValueTask<CacheSyncResult> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return GetCore(await this.cache.GetAsync(key, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.ConfigServiceCacheReadError(ex);
            return new CacheSyncResult(LocalCachedConfig);
        }
    }

    private CacheSyncResult GetCore(string? externalSerializedConfig)
    {
        ProjectConfig oldCachedConfig, newCachedConfig;

        lock (this.syncObj)
        {
            if (externalSerializedConfig is null || externalSerializedConfig == this.cachedSerializedConfig)
            {
                return new CacheSyncResult(this.cachedConfig);
            }

            oldCachedConfig = this.cachedConfig;
            this.cachedConfig = newCachedConfig = ProjectConfig.Deserialize(externalSerializedConfig);
            this.cachedSerializedConfig = externalSerializedConfig;
        }

        var hasChanged = !ProjectConfig.ContentEquals(newCachedConfig, oldCachedConfig);
        return new CacheSyncResult(newCachedConfig, hasChanged);
    }

    public override async ValueTask SetAsync(string key, ProjectConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            if (SetCore(config) is { } serializedConfig)
            {
                await this.cache.SetAsync(key, serializedConfig, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.ConfigServiceCacheWriteError(ex);
        }
    }

    private string? SetCore(ProjectConfig config)
    {
        lock (this.syncObj)
        {
            if (!config.IsEmpty)
            {
                this.cachedSerializedConfig = ProjectConfig.Serialize(config);
            }
            else
            {
                // We may have empty entries with TimeStamp > DateTime.MinValue (see the flooding prevention logic in DefaultConfigFetcher).
                // In such cases we want to preserve the TimeStamp locally but don't want to store those entries into the external cache.
                this.cachedSerializedConfig = null;
            }

            this.cachedConfig = config;

            return this.cachedSerializedConfig;
        }
    }
}
