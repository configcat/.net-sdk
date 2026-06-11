using System;
using System.Runtime.CompilerServices;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Extensions.Hosting;

/// <summary>
/// Defines the union of initialization modes.
/// </summary>
[Union]
public readonly struct ConfigCatInitMode : IUnion
{
    // TODO: Simplify this struct to
    // `public union ConfigCatInitMode(ConfigCatInitMode.DoNotWaitForClientReady, ConfigCatInitMode.WaitForClientReady) { /* ... */ }`
    // as soon as we upgrade to C# 15.

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigCatInitMode"/> struct for the <see cref="DoNotWaitForClientReady"/> case.
    /// </summary>
    public ConfigCatInitMode(DoNotWaitForClientReady value) => Value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigCatInitMode"/> struct for the <see cref="WaitForClientReady"/> case.
    /// </summary>
    public ConfigCatInitMode(WaitForClientReady value) => Value = value;

    /// <inheritdoc/>
    public object? Value { get; }

    /// <summary>
    /// Represents an initialization mode where the initializer service creates client instances at application startup by resolving the <see cref="IConfigCatClient"/>
    /// services from the DI container but does not wait for the clients to reach the ready state (see also <seealso cref="IProvidesHooks.ClientReady"/>).
    /// </summary>
    public sealed class DoNotWaitForClientReady()
    {
        // The explicit conversion is provided for projects using an earlier language version than C# 15.

        /// <summary>
        /// Converts the instance to a <see cref="ConfigCatInitMode"/> union.
        /// </summary>
        public static implicit operator ConfigCatInitMode(DoNotWaitForClientReady value) => new ConfigCatInitMode(value);
    }

    /// <summary>
    /// Represents an initialization mode where the initializer service creates client instances at application startup by resolving the <see cref="IConfigCatClient"/>
    /// services from the DI container and waits for all clients to reach the ready state (see also <seealso cref="IProvidesHooks.ClientReady"/>).
    /// </summary>
    /// <param name="throwOnFailure">
    /// Specifies whether to throw a <see cref="TimeoutException"/> during initialization, thus, to terminate the application
    /// if one or more clients using Auto Polling mode fail to obtain config data within the configured <see cref="AutoPoll.MaxInitWaitTime"/>.
    /// Defaults to <see langword="false"/>.
    /// </param>
    public sealed class WaitForClientReady(bool throwOnFailure = false)
    {
        /// <summary>
        /// Indicates whether to throw a <see cref="TimeoutException"/> during initialization, thus, to terminate the application
        /// if one or more clients using Auto Polling mode fail to obtain config data within the configured <see cref="AutoPoll.MaxInitWaitTime"/>.
        /// </summary>
        public bool ThrowOnFailure { get; } = throwOnFailure;

        // The explicit conversion is provided for projects using an earlier language version than C# 15.

        /// <summary>
        /// Converts the instance to a <see cref="ConfigCatInitMode"/> union.
        /// </summary>
        public static implicit operator ConfigCatInitMode(WaitForClientReady value) => new ConfigCatInitMode(value);
    }
}
