using System;
using System.Collections.Generic;
using System.Linq;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client;

internal sealed class ConfigCatClientCache
{
    internal readonly Dictionary<string, WeakReference<ConfigCatClient>> instances = new();

    public ConfigCatClient GetOrCreate(string sdkKey, ConfigCatClientOptions options, out bool instanceAlreadyCreated)
    {
        lock (this.instances)
        {
            ConfigCatClient? instance;

            if (!this.instances.TryGetValue(sdkKey, out var weakRef))
            {
                instanceAlreadyCreated = false;
                instance = new ConfigCatClient(sdkKey, options);
                weakRef = new WeakReference<ConfigCatClient>(instance);
                this.instances.Add(sdkKey, weakRef);
            }
            else if (!weakRef.TryGetTarget(out instance))
            {
                instanceAlreadyCreated = false;
                instance = new ConfigCatClient(sdkKey, options);
                weakRef.SetTarget(instance);
            }
            else
            {
                instanceAlreadyCreated = true;
            }

            return instance;
        }
    }

    public bool Remove(string sdkKey, ConfigCatClient? instanceToRemove)
    {
        lock (this.instances)
        {
            if (this.instances.TryGetValue(sdkKey, out var weakRef))
            {
                var instanceIsAvailable = weakRef.TryGetTarget(out var instance);
                if (!instanceIsAvailable || ReferenceEquals(instance, instanceToRemove))
                {
                    this.instances.Remove(sdkKey);
                    return instanceIsAvailable;
                }
            }

            return false;
        }
    }

    public void Clear(out ConfigCatClient[] removedInstances)
    {
        lock (this.instances)
        {
            removedInstances = this.instances.Values
                .Select(weakRef => weakRef.TryGetTarget(out var instance) ? instance : null)
                .Where(instance => instance is not null)
                .ToArray()!;

            this.instances.Clear();
        }
    }
}
