using System;
using ConfigCat.HostingIntegration;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigCatServiceCollectionExtensions
{
    private static IServiceCollection AddConfigCatCore(this IServiceCollection services, IConfiguration? configuration, Action<ConfigCatBuilder>? configureConfigCat)
    {
        if (configureConfigCat is null)
        {
            throw new ArgumentNullException(nameof(configureConfigCat));
        }

        var builder = new ConfigCatBuilder();
        configureConfigCat?.Invoke(builder);
        services.AddOptions();
        return builder.Build(services, configuration);
    }

    public static IServiceCollection AddConfigCat(this IServiceCollection services, Action<ConfigCatBuilder> configureConfigCat)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureConfigCat is null)
        {
            throw new ArgumentNullException(nameof(configureConfigCat));
        }

        return services.AddConfigCatCore(configuration: null, configureConfigCat);
    }

    public static IServiceCollection AddConfigCat(this IServiceCollection services, IConfiguration configuration)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return services.AddConfigCatCore(configuration, configureConfigCat: null);
    }

    public static IServiceCollection AddConfigCat(this IServiceCollection services, IConfiguration configuration, Action<ConfigCatBuilder> configureConfigCat)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (configureConfigCat is null)
        {
            throw new ArgumentNullException(nameof(configureConfigCat));
        }

        return services.AddConfigCatCore(configuration, configureConfigCat);
    }
}
