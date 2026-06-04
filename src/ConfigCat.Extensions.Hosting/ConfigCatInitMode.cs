using ConfigCat.Client;

namespace ConfigCat.Extensions.Hosting;

/// <summary>
/// Specifies how to initialize the ConfigCat services at application startup.
/// </summary>
public enum ConfigCatInitMode
{
    /// <summary>
    /// The initializer service should create client instances at application startup by resolving the <see cref="IConfigCatClient"/>
    /// services from the DI container but should not wait for the clients to reach the ready state.
    /// </summary>
    DoNotWaitForClientReady,
    /// <summary>
    /// The initializer service should create client instances at application startup by resolving the <see cref="IConfigCatClient"/>
    /// services from the DI container and should wait for all clients to reach the ready state.
    /// </summary>
    WaitForClientReady,
}
