namespace ConfigCat.Client.Cache;

internal readonly struct CacheParameters
{
    public CacheParameters(IConfigCatCache configCache, string cacheKey)
    {
        ConfigCache = configCache;
        CacheKey = cacheKey;
    }

    public IConfigCatCache ConfigCache { get; }

    public string CacheKey { get; }
}
