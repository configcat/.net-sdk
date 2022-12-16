using System;
using System.Collections.Generic;
using System.Linq;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client
{
    internal sealed class ConfigCatClientCache
    {
        internal readonly Dictionary<string, WeakReference<ConfigCatClient>> instances = new();

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

        public bool Remove(string sdkKey, ConfigCatClient instanceToRemove)
        {
            lock (instances)
            {
                if (instances.TryGetValue(sdkKey, out var weakRef))
                {
                    var instanceIsAvailable = weakRef.TryGetTarget(out var instance);
                    if (!instanceIsAvailable || ReferenceEquals(instance, instanceToRemove))
                    {
                        instances.Remove(sdkKey);
                        return instanceIsAvailable;
                    }
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
