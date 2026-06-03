using System;
using ConfigCat.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class ConfigCatHostBuilderExtensions
{
    private static IServiceCollection AddHostingSpecificConfigCatServices(this IServiceCollection services)
    {
        // NOTE: Potential multiple registrations are not a problem as AddHostedService() ensures that a specific
        // hosted service implementation is added only once.
        return services.AddHostedService<ConfigCatInitService>();
    }

    private static IServiceCollection AddConfigCat(this IServiceCollection services, HostBuilderContext context, Action<ConfigCatBuilder>? configureConfigCat)
    {
        var builder = new ConfigCatBuilder();
        configureConfigCat?.Invoke(builder);
        services.AddHostingSpecificConfigCatServices();
        return builder.Build(services, context.Configuration);
    }

    public static IHostBuilder ConfigureConfigCat(this IHostBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureServices((context, services) => services.AddConfigCat(context, configureConfigCat: null));
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

    public static ConfigCatBuilder UseConfigCat(this IHostApplicationBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddHostingSpecificConfigCatServices();
        return new ConfigCatBuilder(builder.Services, builder.Configuration);
    }
}
