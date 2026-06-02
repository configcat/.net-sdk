using System;

namespace ConfigCat.HostingIntegration.Configuration;

public sealed class ConfigCatInitializerOptions
{
    public ConfigCatInitMode Mode
    {
        get => field;
        set => field = value is >= ConfigCatInitMode.DoNotWaitForClientReady and <= ConfigCatInitMode.WaitForClientReady
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, null!);
    }

    public bool ThrowOnFailure { get; set; }
}
