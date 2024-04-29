namespace ConfigCat.Client;

/// <summary>
/// Specifies the possible states of the local cache.
/// </summary>
public enum ClientCacheState
{
    /// <summary>
    /// No feature flag data is available in the local cache.
    /// </summary>
    NoFlagData,

    /// <summary>
    /// Feature flag data provided by local flag override is only available in the local cache.
    /// </summary>
    HasLocalOverrideFlagDataOnly,

    /// <summary>
    /// Out-of-date feature flag data downloaded from the remote server is available in the local cache.
    /// </summary>
    HasCachedFlagDataOnly,

    /// <summary>
    /// Up-to-date feature flag data downloaded from the remote server is available in the local cache.
    /// </summary>
    HasUpToDateFlagData,
}
