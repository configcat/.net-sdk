using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigCat.HostingIntegration;

public sealed class ConfigCatInitArgs
{
    internal const ConfigCatInitStrategy UnsetInitStrategy = (ConfigCatInitStrategy)(-1);
    internal const ConfigCatInitStrategy DefaultInitStrategy = ConfigCatInitStrategy.DoNotInitializeClients;

    internal static bool IsValidInitStrategy(ConfigCatInitStrategy initStrategy) =>
        initStrategy is >= ConfigCatInitStrategy.DoNotInitializeClients and <= ConfigCatInitStrategy.WaitForClientReadyAndThrowOnFailure;

    public static ConfigCatInitArgs From(IServiceCollection services, ConfigCatInitStrategy initStrategy = DefaultInitStrategy)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (!IsValidInitStrategy(initStrategy))
        {
            throw new ArgumentOutOfRangeException(nameof(initStrategy), initStrategy, null!);
        }

        return new ConfigCatInitArgs(ConfigCatBuilder.GetClientNamesFrom(services), initStrategy);
    }

    internal ConfigCatInitArgs(IReadOnlyCollection<string> clientNames, ConfigCatInitStrategy initStrategy)
    {
        Debug.Assert(IsValidInitStrategy(initStrategy));

        ClientNames = clientNames;
        InitStrategy = initStrategy;
    }

    public IReadOnlyCollection<string> ClientNames { get; }

    public ConfigCatInitStrategy InitStrategy { get; }
}
