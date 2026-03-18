using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigCat.HostingIntegration.Configuration;

public sealed class ConfigCatInitializerOptions
{
    internal static bool IsValidInitStrategy(ConfigCatInitStrategy initStrategy) =>
        initStrategy is >= ConfigCatInitStrategy.DoNotInitializeClients and <= ConfigCatInitStrategy.WaitForClientReadyAndThrowOnFailure;

    public ConfigCatInitStrategy InitStrategy
    {
        get => field;
        set => field = IsValidInitStrategy(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, null!);
    }

    private IReadOnlyCollection<string>? clientNames;

    // NOTE: We declare get/set methods instead of a property for clientNames as we don't want it to be bindable from configuration.

    public IReadOnlyCollection<string>? GetClientNames() => this.clientNames;

    public void SetClientNames(IReadOnlyCollection<string>? clientNames) => this.clientNames = clientNames;
}
