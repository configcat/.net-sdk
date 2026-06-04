using System;
using ConfigCat.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for adding ConfigCat SDK services to an <see cref="IHostBuilder"/> or <see cref="IHostApplicationBuilder"/>.
/// </summary>
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

    /// <summary>
    /// Adds the ConfigCat SDK services to the host based on the <c>ConfigCat</c> section of the application's configuration.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
    /// <returns>The <see cref="IHostBuilder"/> so that additional calls can be chained.</returns>
    public static IHostBuilder ConfigureConfigCat(this IHostBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureServices((context, services) => services.AddConfigCat(context, configureConfigCat: null));
    }

    /// <summary>
    /// Adds the ConfigCat SDK services to the host based on the <c>ConfigCat</c> section of the application's configuration
    /// and applies the specified callback.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
    /// <param name="configureConfigCat">
    /// A callback for further configuring the ConfigCat SDK services. Settings applied here override those read from configuration.
    /// </param>
    /// <returns>The <see cref="IHostBuilder"/> so that additional calls can be chained.</returns>
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

    /// <summary>
    /// Adds the ConfigCat SDK services to the host based on the <c>ConfigCat</c> section of the application's configuration
    /// and applies the specified callback.
    /// </summary>
    /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
    /// <param name="configureConfigCat">
    /// A callback for further configuring the ConfigCat SDK services. The <see cref="HostBuilderContext"/> parameter provides access
    /// to the application's configuration and environment. Settings applied here override those read from configuration.
    /// </param>
    /// <returns>The <see cref="IHostBuilder"/> so that additional calls can be chained.</returns>
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

    /// <summary>
    /// Adds the ConfigCat SDK services to the host based on the <c>ConfigCat</c> section of the application's configuration.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <returns>A <see cref="ConfigCatBuilder"/> that can be used to further configure the ConfigCat SDK services,
    /// overriding the settings read from configuration.</returns>
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
