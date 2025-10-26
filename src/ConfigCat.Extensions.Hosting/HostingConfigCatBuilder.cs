using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConfigCat.Extensions.Hosting;

public sealed class HostingConfigCatBuilder : ConfigCatBuilder<HostingConfigCatBuilder>
{
    private const ConfigCatInitStrategy UnsetInitStrategy = (ConfigCatInitStrategy)(-1);
    private const ConfigCatInitStrategy DefaultInitStrategy = ConfigCatInitStrategy.DoNotWaitForClientReady;

    private ConfigCatInitStrategy initStrategy = UnsetInitStrategy;

    internal HostingConfigCatBuilder()
        : base() { }

    internal HostingConfigCatBuilder(IHostApplicationBuilder hostApplicationBuilder)
        : base(hostApplicationBuilder.Services, hostApplicationBuilder.Configuration) { }

    private void RegisterInitServiceIfNecessary(IServiceCollection services)
    {
        Debug.Assert(this.initStrategy != UnsetInitStrategy);

        if (this.initStrategy != ConfigCatInitStrategy.DoNotWaitForClientReady)
        {
            services.AddHostedService(sp => new ConfigCatInitService(sp, ClientKeys, this.initStrategy));
        }
    }

    public HostingConfigCatBuilder UseInitStrategy(ConfigCatInitStrategy initStrategy)
    {
        if (initStrategy is < ConfigCatInitStrategy.DoNotWaitForClientReady or > ConfigCatInitStrategy.WaitForClientReadyAndThrowOnFailure)
        {
            throw new ArgumentOutOfRangeException(nameof(initStrategy), initStrategy, null!);
        }

        if (this.initStrategy != initStrategy)
        {
            if (this.services is not null && this.initStrategy is not (UnsetInitStrategy or ConfigCatInitStrategy.DoNotWaitForClientReady))
            {
                RemoveLast(this.services, descriptor => descriptor.ImplementationFactory?.Method.ReturnType == typeof(ConfigCatInitService));
            }

            this.initStrategy = initStrategy;

            if (this.services is not null)
            {
                RegisterInitServiceIfNecessary(this.services);
            }
        }

        return this;
    }

    private protected override void OnClientRegistered(IServiceCollection services)
    {
        if (this.initStrategy == UnsetInitStrategy)
        {
            this.initStrategy = DefaultInitStrategy;
        }

        RegisterInitServiceIfNecessary(services);
    }

    internal IServiceCollection Build(IServiceCollection services, HostBuilderContext context)
        => Build(services, context.Configuration);
}
