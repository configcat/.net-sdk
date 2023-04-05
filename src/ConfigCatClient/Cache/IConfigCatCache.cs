using System.Threading.Tasks;
using System.Threading;

namespace ConfigCat.Client;

/// <summary>
/// Defines a cache used by the <see cref="ConfigCatClient"/>.
/// </summary>
public interface IConfigCatCache
{
    /// <summary>
    /// Sets a <see cref="ProjectConfig"/> into cache.
    /// </summary>
    /// <param name="key">A string identifying the <see cref="ProjectConfig"/> value.</param>
    /// <param name="config">The config to cache.</param>
    void Set(string key, ProjectConfig config);

    /// <summary>
    /// Sets a <see cref="ProjectConfig"/> into cache.
    /// </summary>
    /// <param name="key">A string identifying the <see cref="ProjectConfig"/> value.</param>
    /// <param name="config">The config to cache.</param>
    Task SetAsync(string key, ProjectConfig config);

    /// <summary>
    /// Gets a <see cref="ProjectConfig"/> from cache.
    /// </summary>
    /// <param name="key">A string identifying the <see cref="ProjectConfig"/> value.</param>
    /// <returns>The cached config or <see cref="ProjectConfig.Empty"/> if there is none.</returns>
    ProjectConfig Get(string key);

    /// <summary>
    /// Gets a <see cref="ProjectConfig"/> from cache.
    /// </summary>
    /// <param name="key">A string identifying the <see cref="ProjectConfig"/> value.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>The cached config or <see cref="ProjectConfig.Empty"/> if there is none.</returns>
    Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default);
}
