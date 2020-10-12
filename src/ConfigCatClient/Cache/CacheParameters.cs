namespace ConfigCat.Client.Cache
{
    internal class CacheParameters
    {
        public IConfigCache ConfigCache { get; set; }

        public string CacheKey { get; set; }
    }
}
