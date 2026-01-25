using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConfigCat.HostingIntegration;

using static ConfigCatBuilder;

public sealed class HostingConfigCatBuilder : ConfigCatBuilder<HostingConfigCatBuilder>
{
    private ConfigCatInitStrategy initStrategy = ConfigCatInitArgs.UnsetInitStrategy;

    internal HostingConfigCatBuilder() // for legacy hosting model
        : base() { }

    internal HostingConfigCatBuilder(IHostApplicationBuilder hostApplicationBuilder) // for minimal hosting model
        : base(hostApplicationBuilder.Services, hostApplicationBuilder.Configuration) { }

    public HostingConfigCatBuilder UseInitStrategy(ConfigCatInitStrategy initStrategy)
    {
        if (!ConfigCatInitArgs.IsValidInitStrategy(initStrategy))
        {
            throw new ArgumentOutOfRangeException(nameof(initStrategy), initStrategy, null!);
        }

        if (this.services is not null)
        {
            var initServiceFactory = FindInitServiceFactory(this.services)!;
            initServiceFactory.InitStrategy = initStrategy;
        }

        this.initStrategy = initStrategy;

        return this;
    }

    internal IServiceCollection Build(IServiceCollection services, HostBuilderContext context)
        => Build(services, context.Configuration);

    private protected override void RegisterBaseServices(IServiceCollection services, IConfiguration? configuration)
    {
        if (FindInitServiceFactory(services) is not { } initServiceFactory)
        {
            initServiceFactory = new InitServiceFactory(services) { InitStrategy = this.initStrategy };
            services.AddHostedService(initServiceFactory.CreateInitService);
        }
        else if (this.initStrategy != ConfigCatInitArgs.UnsetInitStrategy)
        {
            initServiceFactory.InitStrategy = this.initStrategy;
        }

        base.RegisterBaseServices(services, configuration);
    }

    private static InitServiceFactory? FindInitServiceFactory(IServiceCollection services)
    {
        var existingHostedService = FindLast(services,
            descriptor =>
                !descriptor.IsKeyedService
                && descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationFactory?.Target is InitServiceFactory,
            out _);

        return (InitServiceFactory?)existingHostedService?.ImplementationFactory!.Target;
    }

    private sealed class InitServiceFactory(IServiceCollection services)
    {
        private object servicesOrClientNames = services; // either an IServiceCollection or IReadOnlyCollection<string>

        public ConfigCatInitStrategy InitStrategy;

        public ConfigCatInitService CreateInitService(IServiceProvider serviceProvider)
        {
            if (this.servicesOrClientNames is not IReadOnlyCollection<string> clientNames)
            {
                // NOTE: Overwriting this field unreferences the service collection so GC can clean it up.
                this.servicesOrClientNames = clientNames = GetClientNamesFrom((IServiceCollection)this.servicesOrClientNames);
            }

            var effectiveInitStrategy = this.InitStrategy != ConfigCatInitArgs.UnsetInitStrategy
                ? this.InitStrategy
                : ConfigCatInitArgs.DefaultInitStrategy;

            var args = new ConfigCatInitArgs(clientNames, effectiveInitStrategy);

            return new ConfigCatInitService(serviceProvider.GetRequiredService<IConfigCatInitializer>(), args);
        }
    }
}
