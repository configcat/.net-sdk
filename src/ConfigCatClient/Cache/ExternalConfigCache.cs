using System;
using System.Threading;
using System.Threading.Tasks;

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

    public override ProjectConfig Get(string key)
    {
        try
        {
            return GetCore(this.cache.Get(key));
        }
        catch (Exception ex)
        {
            this.logger.ConfigServiceCacheReadError(ex);
            return LocalCachedConfig;
        }
    }

    public override async Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return GetCore(await this.cache.GetAsync(key, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            this.logger.ConfigServiceCacheReadError(ex);
            return LocalCachedConfig;
        }
    }

    private ProjectConfig GetCore(string? serializedConfig)
    {
        lock (this.syncObj)
        {
            if (serializedConfig is null || serializedConfig == this.cachedSerializedConfig)
            {
                return this.cachedConfig;
            }
        }

        var config = ProjectConfig.Deserialize(serializedConfig);

        lock (this.syncObj)
        {
            if (config.IsNewerThan(this.cachedConfig))
            {
                this.cachedConfig = config;
                this.cachedSerializedConfig = serializedConfig;
            }
            return this.cachedConfig;
        }
    }

    public override void Set(string key, ProjectConfig config)
    {
        try
        {
            if (SetCore(config) is { } serializedConfig)
            {
                this.cache.Set(key, serializedConfig);
            }
        }
        catch (Exception ex)
        {
            this.logger.ConfigServiceCacheWriteError(ex);
        }
    }

    public override async Task SetAsync(string key, ProjectConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            if (SetCore(config) is { } serializedConfig)
            {
                await this.cache.SetAsync(key, serializedConfig, cancellationToken).ConfigureAwait(false);
            }
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
            if (!config.IsNewerThan(this.cachedConfig))
            {
                return null;
            }
            else if (config.IsEmpty)
            {
                // We may have empty entries with TimeStamp > DateTime.MinValue (see the flooding prevention logic in HttpConfigFetcher).
                // In such cases we want to preserve the TimeStamp locally but don't want to store those entries into the external cache.
                this.cachedConfig = config;
                return this.cachedSerializedConfig = null;
            }
        }

        var serializedConfig = ProjectConfig.Serialize(config);

        lock (this.syncObj)
        {
            if (config.IsNewerThan(this.cachedConfig))
            {
                this.cachedConfig = config;
                return this.cachedSerializedConfig = serializedConfig;
            }
            return null;
        }
    }
}
