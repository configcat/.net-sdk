using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides data for the <see cref="ConfigCatClient.ClientReady"/> event.
/// </summary>
public class ClientReadyEventArgs : EventArgs
{
    internal ClientReadyEventArgs(ClientCacheState cacheState)
    {
        CacheState = cacheState;
    }

    /// <summary>
    /// The state of the local cache at the time the initialization was completed.
    /// </summary>
    public ClientCacheState CacheState { get; }
}
