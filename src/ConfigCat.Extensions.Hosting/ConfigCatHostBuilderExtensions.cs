using System;
using ConfigCat.Extensions.Hosting;

namespace Microsoft.Extensions.Hosting;

public static class ConfigCatHostBuilderExtensions
{
    public static ConfigCatBuilder UseConfigCat(this IHostBuilder builder,
        ConfigCatInitStrategy initStrategy = ConfigCatInitStrategy.WaitForClientReadyAndLogOnFailure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return new ConfigCatBuilder(builder, initStrategy);
    }

    public static ConfigCatBuilder UseConfigCat(this IHostApplicationBuilder builder,
        ConfigCatInitStrategy initStrategy = ConfigCatInitStrategy.WaitForClientReadyAndLogOnFailure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return new ConfigCatBuilder(builder, initStrategy);
    }
}
