using System.Linq;

namespace ConfigCat.Client
{
    internal static class ConfigCatClientCacheExtensions
    {
        public static int GetAliveCount(this ConfigCatClientCache cache)
        {
            return cache.GetAliveCount(out _);
        }

        public static int GetAliveCount(this ConfigCatClientCache cache, out int cacheSize)
        {
            lock (cache.instances)
            {
                cacheSize = cache.instances.Count;
                return cache.instances.Values.Count(weakRef => weakRef.TryGetTarget(out _));
            }
        }
    }
}
