namespace ConfigCat.Client;

/// <summary>
/// Describes how the overrides should behave.
/// </summary>
public enum OverrideBehaviour
{
    /// <summary>
    /// When evaluating values, the SDK will not use feature flags and settings from the ConfigCat CDN, but it will use
    /// all feature flags and settings that are loaded from local-override sources.
    /// </summary>
    LocalOnly,

    /// <summary>
    /// When evaluating values, the SDK will use all feature flags and settings that are downloaded from the ConfigCat CDN,
    /// plus all feature flags and settings that are loaded from local-override sources. If a feature flag or a setting is
    /// defined both in the fetched and the local-override source then the local-override version will take precedence.
    /// </summary>
    LocalOverRemote,

    /// <summary>
    /// When evaluating values, the SDK will use all feature flags and settings that are downloaded from the ConfigCat CDN,
    /// plus all feature flags and settings that are loaded from local-override sources. If a feature flag or a setting is
    /// defined both in the fetched and the local-override source then the fetched version will take precedence.
    /// </summary>
    RemoteOverLocal,
}
