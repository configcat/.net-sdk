using System;
using ConfigCat.HostingIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class ConfigCatHostBuilderExtensions
{
    private static IServiceCollection AddConfigCat(this IServiceCollection services, HostBuilderContext context, Action<HostingConfigCatBuilder> configureConfigCat)
    {
        var builder = new HostingConfigCatBuilder();
        configureConfigCat(builder);
        return builder.Build(services, context);
    }

    public static IHostBuilder ConfigureConfigCat(this IHostBuilder builder, Action<HostBuilderContext, HostingConfigCatBuilder> configureConfigCat)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configureConfigCat is null)
        {
            throw new ArgumentNullException(nameof(configureConfigCat));
        }

        return builder.ConfigureServices((context, services) => services.AddConfigCat(context, builder => configureConfigCat(context, builder)));
    }

    public static IHostBuilder ConfigureConfigCat(this IHostBuilder builder, Action<HostingConfigCatBuilder> configureConfigCat)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configureConfigCat is null)
        {
            throw new ArgumentNullException(nameof(configureConfigCat));
        }

        return builder.ConfigureServices((context, services) => services.AddConfigCat(context, configureConfigCat));
    }

    public static HostingConfigCatBuilder UseConfigCat(this IHostApplicationBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return new HostingConfigCatBuilder(builder);
    }
}
