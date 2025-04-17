using System;
using ConfigCat.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class ConfigCatHostBuilderExtensions
{
    private static IServiceCollection AddConfigCat(this IServiceCollection services, HostBuilderContext context, Action<ConfigCatBuilder> configureConfigCat)
    {
        var builder = new ConfigCatBuilder();
        configureConfigCat(builder);
        return builder.Build(services, context);
    }

    public static IHostBuilder ConfigureConfigCat(this IHostBuilder builder, Action<HostBuilderContext, ConfigCatBuilder> configureConfigCat)
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

    public static IHostBuilder ConfigureConfigCat(this IHostBuilder builder, Action<ConfigCatBuilder> configureConfigCat)
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

    public static ConfigCatBuilder UseConfigCat(this IHostApplicationBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return new ConfigCatBuilder(builder);
    }
}
