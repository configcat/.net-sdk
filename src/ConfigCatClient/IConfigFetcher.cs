using System.Threading.Tasks;

namespace ConfigCat.Client
{
    /// <summary>
    /// Defines configuration fetch
    /// </summary>
    internal interface IConfigFetcher
    {
        /// <summary>
        /// Fetch the configuration
        /// </summary>
        /// <param name="lastConfig">Last of fetched configuration if it is present</param>
        /// <returns></returns>
        Task<ProjectConfig> Fetch(ProjectConfig lastConfig);
    }
}