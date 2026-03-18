using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ConfigCat.Client;
using ConfigCat.HostingIntegration.Adapters;
using ConfigCat.HostingIntegration.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigCat.HostingIntegration;

using static ConfigCatBuilder;

public abstract class ConfigCatBuilder<TBuilder> where TBuilder : ConfigCatBuilder<TBuilder>
{
    private protected readonly IServiceCollection? services; // set for minimal hosting model only
    private protected readonly IConfiguration? configuration; // set for minimal hosting model only

    private Action<IServiceCollection>? pendingRegistrations;

    private protected ConfigCatBuilder() { } // for legacy hosting model and plain DI setup

    private protected ConfigCatBuilder(IServiceCollection services, IConfiguration? configuration) // for minimal hosting model
    {
        this.services = services;
        this.configuration = configuration;

        RegisterBaseServices(services, configuration);
    }

    public TBuilder AddDefaultClient(Action<ExtendedConfigCatClientOptions> configureOptions)
    {
        if (configureOptions is null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        if (this.services is not null)
        {
            RegisterDefaultClient(this.services, new ConfigureNamedOptions<ExtendedConfigCatClientOptions>(Options.DefaultName, configureOptions));
        }
        else
        {
            this.pendingRegistrations += services =>
                RegisterDefaultClient(services, new ConfigureNamedOptions<ExtendedConfigCatClientOptions>(Options.DefaultName, configureOptions));
        }

        return (TBuilder)this;
    }

    public TBuilder AddNamedClient(string clientName, Action<ExtendedConfigCatClientOptions> configureOptions)
    {
        ValidateClientName(clientName);

        if (configureOptions is null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        if (this.services is not null)
        {
            RegisterNamedClient(this.services, clientName, new ConfigureNamedOptions<ExtendedConfigCatClientOptions>(clientName, configureOptions));
        }
        else
        {
            this.pendingRegistrations += services =>
                RegisterNamedClient(services, clientName, new ConfigureNamedOptions<ExtendedConfigCatClientOptions>(clientName, configureOptions));
        }

        return (TBuilder)this;
    }

    internal IServiceCollection Build(IServiceCollection services, IConfiguration? configuration)
    {
        RegisterBaseServices(services, configuration);

        this.pendingRegistrations?.Invoke(services);

        return services;
    }

    private protected virtual void RegisterBaseServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.TryAddSingleton<IConfigCatInitializer, ConfigCatInitializer>();

        services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>, ConfigureCommonClientOptions>();

        var section = GetConfigurationSection(configuration);
        if (section.Exists())
        {
            var defaultClientSection = section.GetSection("DefaultClient");
            if (defaultClientSection.Exists())
            {
                RegisterDefaultClient(services, new ConfigureClientOptions(Options.DefaultName, defaultClientSection));
            }

            foreach (var namedClientSection in section.GetSection("NamedClients").GetChildren())
            {
                var clientName = namedClientSection.Key;
                ValidateClientName(clientName);
                RegisterNamedClient(services, clientName, new ConfigureClientOptions(clientName, namedClientSection));
            }
        }
    }

    private protected static IConfigurationSection? GetConfigurationSection(IConfiguration? configuration) =>
        configuration?.GetSection("ConfigCat");
}

public sealed class ConfigCatBuilder : ConfigCatBuilder<ConfigCatBuilder>
{
    internal ConfigCatBuilder() { } // for plain DI setup

    private protected override void RegisterBaseServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.AddOptions();

        base.RegisterBaseServices(services, configuration);
    }

    internal static void ValidateClientName(string clientName)
    {
        if (clientName is null)
        {
            throw new ArgumentNullException(nameof(clientName));
        }

        if (clientName == Options.DefaultName)
        {
            throw new ArgumentException($"Client name cannot be \"{clientName}\".", nameof(clientName));
        }
    }

    internal static void RegisterDefaultClient(IServiceCollection services, IConfigureOptions<ExtendedConfigCatClientOptions> configureOptions)
    {
        static bool IsExistingRegistration<TFactory>(ServiceDescriptor descriptor) where TFactory : ClientFactoryBase =>
            !descriptor.IsKeyedService
            && descriptor.ImplementationFactory?.Target is TFactory;

        RemoveLast(services, IsExistingRegistration<ClientFactory>);
        RemoveLast(services, IsExistingRegistration<ClientSnapshotFactory>);

        services.AddSingleton<IConfigCatClient>(new ClientFactory(clientName: Options.DefaultName).CreateDefault);
        services.AddScoped<IConfigCatClientSnapshot>(new ClientSnapshotFactory(clientName: Options.DefaultName).CreateDefault);

        services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(configureOptions);
    }

    internal static void RegisterNamedClient(IServiceCollection services, string clientName, IConfigureOptions<ExtendedConfigCatClientOptions> configureOptions)
    {
        bool IsExistingRegistration<TFactory>(ServiceDescriptor descriptor) where TFactory : ClientFactoryBase =>
            descriptor.IsKeyedService
            && descriptor.KeyedImplementationFactory?.Target is TFactory factory
            && factory.ClientName == clientName;

        RemoveLast(services, IsExistingRegistration<ClientFactory>);
        RemoveLast(services, IsExistingRegistration<ClientSnapshotFactory>);

        services.AddKeyedSingleton<IConfigCatClient>(clientName, new ClientFactory(clientName).CreateNamed);
        services.AddKeyedScoped<IConfigCatClientSnapshot>(clientName, new ClientSnapshotFactory(clientName).CreateNamed);

        services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(configureOptions);
    }

    private static string? GetClientNameFrom(ServiceDescriptor serviceDescriptor)
    {
        if (!serviceDescriptor.IsKeyedService)
        {
            if (serviceDescriptor.ImplementationFactory?.Target is ClientFactory)
            {
                return Options.DefaultName;
            }
        }
        else
        {
            if (serviceDescriptor.KeyedImplementationFactory?.Target is ClientFactory factory)
            {
                return factory.ClientName;
            }
        }

        return null;
    }

    internal static IReadOnlyCollection<string> GetClientNamesFrom(IServiceCollection services)
    {
        // NOTE: Currently there is no way to resolve keys for some keyed service from a service provider
        // (see also https://github.com/dotnet/runtime/issues/100105), so we resort to obtaining those
        // from the service collection.

        return services
            .Select(GetClientNameFrom)
            .Where(clientName => clientName is not null)
            .Distinct()
            .ToArray()!;
    }

    internal static void RemoveLast(IServiceCollection services, Func<ServiceDescriptor, bool> match)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var service = services[i];
            if (match(service))
            {
                services.RemoveAt(i);
            }
        }
    }

#pragma warning disable CA1822 // Member '{0}' does not access instance data and can be marked as static

    private abstract class ClientFactoryBase(string clientName)
    {
        public readonly string ClientName = clientName;
    }

    private sealed class ClientFactory(string clientName) : ClientFactoryBase(clientName)
    {
        public IConfigCatClient CreateDefault(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExtendedConfigCatClientOptions>>().Value;
            return ConfigCatClient.Get(options.SdkKey!, options);
        }

        public IConfigCatClient CreateNamed(IServiceProvider serviceProvider, object? name)
        {
            Debug.Assert(Equals(name, this.ClientName));
            ExtendedConfigCatClientOptions options;
            using (var scope = serviceProvider.CreateScope())
            {
                options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ExtendedConfigCatClientOptions>>().Get(name: this.ClientName);
            }
            return ConfigCatClient.Get(options.SdkKey!, options);
        }
    }

    private sealed class ClientSnapshotFactory(string clientName) : ClientFactoryBase(clientName)
    {
        public IConfigCatClientSnapshot CreateDefault(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IConfigCatClient>().Snapshot();
        }

        public IConfigCatClientSnapshot CreateNamed(IServiceProvider serviceProvider, object? name)
        {
            Debug.Assert(Equals(name, this.ClientName));
            return serviceProvider.GetRequiredKeyedService<IConfigCatClient>(this.ClientName).Snapshot();
        }
    }

#pragma warning restore CA1822

    internal sealed class ConfigureClientOptions(string name, IConfiguration configuration)
        : ConfigureNamedOptions<ExtendedConfigCatClientOptions, IConfiguration>(
            name,
            configuration!,
            (options, configuration) =>
            {
                // In .NET 8+ builds configuration binding is source generated (see also csproj).
                configuration.Bind(new ExtendedConfigCatClientOptions.BindingWrapper(options));
            })
    {
    }

    internal sealed class ConfigureCommonClientOptions(ILogger<ConfigCatClient> logger)
        : ConfigureNamedOptions<ExtendedConfigCatClientOptions, ILogger<ConfigCatClient>>(
            name: null, // null means configuring all options (named or unnamed)
            logger,
            (options, logger) =>
            {
                if (logger is not null)
                {
                    options.Logger = new ConfigCatToMSLoggerAdapter(logger);
                }
            })
    {
        public ConfigureCommonClientOptions() : this(null!) { }
    }
}
