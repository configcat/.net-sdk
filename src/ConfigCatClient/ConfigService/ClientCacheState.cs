namespace ConfigCat.Client;

/// <summary>
/// Specifies the possible states of the internal cache.
/// </summary>
public enum ClientCacheState
{
    /// <summary>
    /// No config data is available in the internal cache.
    /// </summary>
    NoFlagData,

    /// <summary>
    /// Only config data provided by local flag override is available in the internal cache.
    /// </summary>
    HasLocalOverrideFlagDataOnly,

    /// <summary>
    /// Only expired config data obtained from the external cache or the ConfigCat CDN is available in the internal cache.
    /// </summary>
    HasCachedFlagDataOnly,

    /// <summary>
    /// Up-to-date config data obtained from the external cache or the ConfigCat CDN is available in the internal cache.
    /// </summary>
    HasUpToDateFlagData,
}
