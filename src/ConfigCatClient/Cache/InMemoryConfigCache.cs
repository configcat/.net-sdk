using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    internal class InMemoryConfigCache : IConfigCatCache
    {
        private ProjectConfig projectConfig;

        /// <inheritdoc />
        public Task SetAsync(string key, ProjectConfig config)
        {
            this.projectConfig = config;
            return Task.FromResult(0);
        }

        /// <inheritdoc />
        public Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(this.projectConfig);

        /// <inheritdoc />
        public void Set(string key, ProjectConfig config) => this.projectConfig = config;

        /// <inheritdoc />
        public ProjectConfig Get(string key) => this.projectConfig;
    }
}