namespace ConfigCat.Client.Cache;

internal readonly struct CacheParameters
{
    public CacheParameters(ConfigCache configCache, string cacheKey)
    {
        ConfigCache = configCache;
        CacheKey = cacheKey;
    }

    public ConfigCache ConfigCache { get; }

    public string CacheKey { get; }
}
