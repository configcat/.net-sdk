namespace ConfigCat.Client.Cache;

internal class CacheParameters
{
    public IConfigCatCache ConfigCache { get; set; }

    public string CacheKey { get; set; }
}
