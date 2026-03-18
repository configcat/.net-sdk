using System;
using System.Collections.Generic;
using ConfigCat.HostingIntegration.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ConfigCat.HostingIntegration;

using static ConfigCatBuilder;

public sealed class HostingConfigCatBuilder : ConfigCatBuilder<HostingConfigCatBuilder>
{
    private const ConfigCatInitStrategy UnsetInitStrategy = (ConfigCatInitStrategy)(-1);

    private ConfigCatInitStrategy initStrategy = UnsetInitStrategy;

    internal HostingConfigCatBuilder() // for legacy hosting model
        : base() { }

    internal HostingConfigCatBuilder(IHostApplicationBuilder hostApplicationBuilder) // for minimal hosting model
        : base(hostApplicationBuilder.Services, hostApplicationBuilder.Configuration) { }

    public HostingConfigCatBuilder UseInitStrategy(ConfigCatInitStrategy initStrategy)
    {
        if (!ConfigCatInitializerOptions.IsValidInitStrategy(initStrategy))
        {
            throw new ArgumentOutOfRangeException(nameof(initStrategy), initStrategy, null!);
        }

        if (this.services is not null)
        {
            ConfigureInitStrategy(this.services, initStrategy);
        }
        else
        {
            this.initStrategy = initStrategy;
        }

        return this;
    }

    private static void ConfigureInitStrategy(IServiceCollection services, ConfigCatInitStrategy initStrategy)
    {
        services.Configure<ConfigCatInitializerOptions>(options => options.InitStrategy = initStrategy);
    }

    internal IServiceCollection Build(IServiceCollection services, HostBuilderContext context)
        => Build(services, context.Configuration);

    private protected override void RegisterBaseServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.AddSingleton<IConfigureOptions<ConfigCatInitializerOptions>>(
            new ConfigureInitializerOptions(services, GetConfigurationSection(configuration)));

        if (this.services is null && this.initStrategy != UnsetInitStrategy)
        {
            ConfigureInitStrategy(services, this.initStrategy);
        }

        // NOTE: Potential multiple registrations are not a problem as AddHostedService() ensures that a specific
        // hosted service implementation is added only once.
        services.AddHostedService<ConfigCatInitService>();

        base.RegisterBaseServices(services, configuration);
    }

    private sealed class ConfigureInitializerOptions(
        IServiceCollection services,
        IConfiguration? configuration)
        : IConfigureOptions<ConfigCatInitializerOptions>
    {
        private object servicesOrClientNames = services; // either an IServiceCollection or IReadOnlyCollection<string>

        public void Configure(ConfigCatInitializerOptions options)
        {
            if (configuration is not null)
            {
                configuration.Bind(options);
            }

            if (this.servicesOrClientNames is not IReadOnlyCollection<string> clientNames)
            {
                // NOTE: Overwriting this field unreferences the service collection so GC can clean it up.
                this.servicesOrClientNames = clientNames = GetClientNamesFrom((IServiceCollection)this.servicesOrClientNames);
            }

            options.SetClientNames(clientNames);
        }
    }
}
