namespace ConfigCat.Client;

/// <summary>
/// Defines a cache used by the <see cref="ConfigCatClient"/>.
/// </summary>
/// <remarks>
/// Note for implementers. Until the deprecated <see cref="IConfigCache"/> interface is removed, this interface needs to extend it for backward compatibility.
/// Later, all of its members will be moved into this interface.
/// </remarks>
public interface IConfigCatCache :
#pragma warning disable CS0618 // Type or member is obsolete
    IConfigCache
#pragma warning restore CS0618 // Type or member is obsolete
{
    /// <summary>
    /// Sets a <see cref="ProjectConfig"/> into cache.
    /// </summary>
    /// <param name="key">A string identifying the <see cref="ProjectConfig"/> value.</param>
    /// <param name="config">The config to cache.</param>
    void Set(string key, ProjectConfig config);

    /// <summary>
    /// Gets a <see cref="ProjectConfig"/> from cache.
    /// </summary>
    /// <returns>The cached config.</returns>
    ProjectConfig Get(string key);
}
