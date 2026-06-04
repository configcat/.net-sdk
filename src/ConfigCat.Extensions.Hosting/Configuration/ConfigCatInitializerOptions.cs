using System;

namespace ConfigCat.Extensions.Hosting.Configuration;

/// <summary>
/// Options for configuring the initialization behavior of the ConfigCat SDK services.
/// </summary>
public sealed class ConfigCatInitializerOptions
{
    /// <summary>
    /// Gets or sets the initialization mode.
    /// </summary>
    public ConfigCatInitMode Mode
    {
        get => field;
        set => field = value is >= ConfigCatInitMode.DoNotWaitForClientReady and <= ConfigCatInitMode.WaitForClientReady
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, null!);
    }

    /// <summary>
    /// Gets or sets whether to throw a <see cref="TimeoutException"/> during initialization, thus, to terminate the application
    /// if one or more clients using Auto Polling mode fail to obtain config data within the configured <c>maxInitWaitTime</c>.
    /// </summary>
    /// <remarks>
    /// Applies only when <see cref="Mode"/> is set to <see cref="ConfigCatInitMode.WaitForClientReady"/>.
    /// </remarks>
    public bool ThrowOnFailure { get; set; }
}
