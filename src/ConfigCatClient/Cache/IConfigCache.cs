namespace ConfigCat.Client
{
    /// <summary>
    /// Defines cache
    /// </summary>
    internal interface IConfigCache
    {
        /// <summary>
        /// Set a <see cref="ProjectConfig"/> into cache
        /// </summary>
        /// <param name="config"></param>
        void Set(ProjectConfig config);

        /// <summary>
        /// Get a <see cref="ProjectConfig"/> from cache
        /// </summary>
        /// <returns></returns>
        ProjectConfig Get();
    }
}