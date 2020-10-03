using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client
{
    internal class InMemoryConfigCache : IConfigCache
    {
        private ProjectConfig projectConfig;

        private readonly ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();

        /// <inheritdoc />
        public Task SetAsync(string key, ProjectConfig config)
        {
            this.lockSlim.EnterWriteLock();

            try
            {
                this.projectConfig = config;
                return Task.FromResult(true);
            }
            finally
            {
                this.lockSlim.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        public Task<ProjectConfig> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            this.lockSlim.EnterReadLock();

            try
            {
                return Task.FromResult(this.projectConfig);
            }
            finally
            {
                this.lockSlim.ExitReadLock();
            }
        }        
    }
}