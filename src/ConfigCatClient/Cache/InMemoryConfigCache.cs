using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    internal class InMemoryConfigCache : IConfigCatCache
    {
        private ProjectConfig projectConfig;
        private readonly ReaderWriterLockSlim lockSlim = new();

        /// <inheritdoc />
        public Task SetAsync(string key, ProjectConfig config)
        {
            this.Set(key, config);
            return Task.FromResult(0);
        }

        /// <inheritdoc />
        public Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(this.Get(key));

        /// <inheritdoc />
        public void Set(string key, ProjectConfig config)
        {
            this.lockSlim.EnterWriteLock();

            try
            {
                this.projectConfig = config;
            }
            finally
            {
                this.lockSlim.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        public ProjectConfig Get(string key)
        {
            this.lockSlim.EnterReadLock();

            try
            {
                return this.projectConfig;
            }
            finally
            {
                this.lockSlim.ExitReadLock();
            }
        }
    }
}