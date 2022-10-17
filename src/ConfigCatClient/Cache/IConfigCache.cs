﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    /// <summary>
    /// Defines cache
    /// </summary>
    [Obsolete("This interface is obsolete and will be removed from the public API in a future major version. Please use the IConfigCatCache interface instead.")]
    public interface IConfigCache
    {
        /// <summary>
        /// Set a <see cref="ProjectConfig"/> into cache
        /// </summary>
        /// <param name="key">A string identifying the <see cref="ProjectConfig"/> value.</param>
        /// <param name="config"></param>
        Task SetAsync(string key, ProjectConfig config);

        /// <summary>
        /// Get a <see cref="ProjectConfig"/> from cache
        /// </summary>
        /// <returns></returns>
        Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default);
    }
}