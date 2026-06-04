using System;
using ConfigCat.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding ConfigCat SDK services to an <see cref="IServiceCollection"/>.
/// </summary>
public static class ConfigCatServiceCollectionExtensions
{
    private static IServiceCollection AddConfigCatCore(this IServiceCollection services, IConfiguration? configuration, Action<ConfigCatBuilder>? configureConfigCat)
    {
        var builder = new ConfigCatBuilder();
        configureConfigCat?.Invoke(builder);
        services.AddOptions();
        return builder.Build(services, configuration);
    }

    /// <summary>
    /// Adds the ConfigCat SDK services to the service collection and applies the specified callback.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureConfigCat">A callback for configuring the ConfigCat SDK services.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
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

    /// <summary>
    /// Adds the ConfigCat SDK services to the service collection based on the <c>ConfigCat</c> section of the specified configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> to read the settings from.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
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

    /// <summary>
    /// Adds the ConfigCat SDK services to the service collection based on the <c>ConfigCat</c> section of the specified configuration
    /// and applies the specified callback.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> to read the settings from.</param>
    /// <param name="configureConfigCat">
    /// A callback for further configuring the ConfigCat SDK services. Settings applied here override those read from configuration.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
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
