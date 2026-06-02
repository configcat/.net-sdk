using System;

namespace ConfigCat.HostingIntegration.Configuration;

public sealed class ConfigCatInitializerOptions
{
    internal static bool IsValidInitStrategy(ConfigCatInitStrategy initStrategy) =>
        initStrategy is >= ConfigCatInitStrategy.DoNotCreateClients and <= ConfigCatInitStrategy.WaitForClientReadyAndThrowOnFailure;

    public ConfigCatInitStrategy InitStrategy
    {
        get => field;
        set => field = IsValidInitStrategy(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, null!);
    }
}
