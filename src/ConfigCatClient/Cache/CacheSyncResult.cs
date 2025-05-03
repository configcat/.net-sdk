namespace ConfigCat.Client.Cache;

internal readonly struct CacheSyncResult
{
    public CacheSyncResult(ProjectConfig config, bool hasChanged = false)
    {
        Config = config;
        HasChanged = hasChanged;
    }

    public ProjectConfig Config { get; }
    public bool HasChanged { get; }
}
