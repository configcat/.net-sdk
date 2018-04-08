using System.Threading;

namespace ConfigCat.Client.Cache
{
    internal class InMemoryConfigCache : IConfigCache
    {
        private ProjectConfig config;

        private readonly ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();

        /// <inheritdoc />
        public void Set(ProjectConfig config)
        {
            this.lockSlim.EnterWriteLock();

            try
            {
                this.config = config;
            }
            finally
            {
                this.lockSlim.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        public ProjectConfig Get()
        {
            this.lockSlim.EnterReadLock();

            try
            {
                return this.config;
            }
            finally
            {
                this.lockSlim.ExitReadLock();
            }
        }        
    }
}