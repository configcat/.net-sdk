using System;
using System.Collections.Generic;
using System.Linq;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client
{
    internal sealed class ConfigCatClientCache
    {
        private readonly Dictionary<string, WeakReference<ConfigCatClient>> instances = new();

        // For testing purposes only
        public int Count
        {
            get
            {
                lock (instances)
                {
                    return instances.Values.Count(weakRef => weakRef.TryGetTarget(out _));
                }
            }
        }

        public ConfigCatClient GetOrCreate(string sdkKey, ConfigCatClientOptions configuration, out bool instanceAlreadyCreated)
        {
            lock (instances)
            {
                ConfigCatClient instance;

                instanceAlreadyCreated = instances.TryGetValue(sdkKey, out var weakRef);
                if (!instanceAlreadyCreated)
                {
                    instance = new ConfigCatClient(sdkKey, configuration);
                    weakRef = new WeakReference<ConfigCatClient>(instance);
                    instances.Add(sdkKey, weakRef);
                }
                else if (!weakRef.TryGetTarget(out instance))
                {
                    instanceAlreadyCreated = false;
                    instance = new ConfigCatClient(sdkKey, configuration);
                    weakRef.SetTarget(instance);
                }

                return instance;
            }
        }

        public bool Remove(string sdkKey, out ConfigCatClient removedInstance)
        {
            lock (instances)
            {
                if (instances.TryGetValue(sdkKey, out var weakRef))
                {
                    instances.Remove(sdkKey);
                    if (weakRef.TryGetTarget(out removedInstance))
                    {
                        return true;
                    }
                }
                else
                {
                    removedInstance = default;
                }

                return false;
            }
        }

        public void Clear(out ConfigCatClient[] removedInstances)
        {
            lock (instances)
            {
                removedInstances = instances.Values
                    .Select(weakRef => weakRef.TryGetTarget(out var instance) ? instance : null)
                    .Where(instance => instance is not null)
                    .ToArray();

                instances.Clear();
            }
        }
    }
}
