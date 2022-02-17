namespace ConfigCat.Client
{
    /// <summary>
    /// Defines a cache used by the <see cref="ConfigCatClient"/>.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public interface IConfigCatCache : IConfigCache // Later, this interface will contain all members of IConfigCache. For backward compatibility, it simply inherits them now.
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
}
