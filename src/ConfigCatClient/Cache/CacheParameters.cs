namespace ConfigCat.Client.Cache
{
    internal class CacheParameters
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public IConfigCache ConfigCache { get; set; } // Backward compatibility, it'll be changed to IConfigCatCache later.
#pragma warning restore CS0618 // Type or member is obsolete

        public string CacheKey { get; set; }
    }
}
