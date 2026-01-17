using System;
using ConfigCat.HostingIntegration;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigCatServiceCollectionExtensions
{
    private static IServiceCollection AddConfigCatCore(this IServiceCollection services, IConfiguration? configuration, Action<ConfigCatBuilder> configureConfigCat)
    {
        if (configureConfigCat is null)
        {
            throw new ArgumentNullException(nameof(configureConfigCat));
        }

        var builder = new ConfigCatBuilder();
        configureConfigCat(builder);
        return builder.Build(services, configuration);
    }

    public static IServiceCollection AddConfigCat(this IServiceCollection services, Action<ConfigCatBuilder> configureConfigCat)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.AddConfigCatCore(configuration: null, configureConfigCat);
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

        return services.AddConfigCatCore(configuration, configureConfigCat);
    }
}
