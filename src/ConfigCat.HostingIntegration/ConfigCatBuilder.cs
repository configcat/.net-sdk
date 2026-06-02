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

public sealed class ConfigCatBuilder
{
    private const ConfigCatInitMode UnsetInitMode = (ConfigCatInitMode)int.MinValue;

    private readonly IServiceCollection? services; // set for minimal hosting model only

    private Action<IServiceCollection>? pendingRegistrations;
    private ConfigCatInitMode initMode = UnsetInitMode;
    private bool throwOnInitFailure;

    internal ConfigCatBuilder() { } // for legacy hosting model and plain DI setup

    internal ConfigCatBuilder(IServiceCollection services, IConfiguration? configuration) // for minimal hosting model
    {
        this.services = services;

        RegisterBaseServices(services, configuration);
    }

    public ConfigCatBuilder AddDefaultClient(Action<ExtendedConfigCatClientOptions> configureOptions)
    {
        if (configureOptions is null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        if (this.services is not null)
        {
            RegisterDefaultClient(this.services, new ConfigureClientOptions(Options.DefaultName, configureOptions));
        }
        else
        {
            this.pendingRegistrations += services =>
                RegisterDefaultClient(services, new ConfigureClientOptions(Options.DefaultName, configureOptions));
        }

        return this;
    }

    public ConfigCatBuilder AddNamedClient(string clientName, Action<ExtendedConfigCatClientOptions> configureOptions)
    {
        ValidateClientName(clientName);

        if (configureOptions is null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        if (this.services is not null)
        {
            RegisterNamedClient(this.services, clientName, new ConfigureClientOptions(clientName, configureOptions));
        }
        else
        {
            this.pendingRegistrations += services =>
                RegisterNamedClient(services, clientName, new ConfigureClientOptions(clientName, configureOptions));
        }

        return this;
    }

    public ConfigCatBuilder DoNotWaitForClientReady()
    {
        if (this.services is not null)
        {
            ConfigureInitializerOptions(this.services, ConfigCatInitMode.DoNotWaitForClientReady, throwOnInitFailure: false);
        }
        else
        {
            this.initMode = ConfigCatInitMode.DoNotWaitForClientReady;
            this.throwOnInitFailure = false;
        }

        return this;
    }

    public ConfigCatBuilder WaitForClientReady(bool throwOnFailure = false)
    {
        if (this.services is not null)
        {
            ConfigureInitializerOptions(this.services, ConfigCatInitMode.WaitForClientReady, throwOnFailure);
        }
        else
        {
            this.initMode = ConfigCatInitMode.WaitForClientReady;
            this.throwOnInitFailure = throwOnFailure;
        }

        return this;
    }

    internal IServiceCollection Build(IServiceCollection services, IConfiguration? configuration)
    {
        RegisterBaseServices(services, configuration);

        this.pendingRegistrations?.Invoke(services);

        return services;
    }

    private void RegisterBaseServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>, ConfigureCommonClientOptions>();

        IConfigurationSection section;
        if (configuration is not null && (section = configuration.GetSection("ConfigCat")).Exists())
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

            var initSection = section.GetSection("Init");
            if (initSection.Exists())
            {
                var configureOptions = new ConfigureNamedOptions<ConfigCatInitializerOptions, IConfiguration>(
                    Options.DefaultName, initSection, (options, configuration) =>
                    {
                        // In .NET 8+ builds, configuration binding is source generated (see also csproj).
                        configuration.Bind(options);
                    });

                services.AddSingleton<IConfigureOptions<ConfigCatInitializerOptions>>(configureOptions);
            }
        }

        if (this.services is null && this.initMode != UnsetInitMode)
        {
            ConfigureInitializerOptions(services, this.initMode, this.throwOnInitFailure);
        }

        services.TryAddSingleton<IConfigCatInitializer, ConfigCatInitializer>();
    }

    private static void ValidateClientName(string clientName)
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

    private static void RegisterDefaultClient(IServiceCollection services, ConfigureClientOptions configureOptions)
    {
        static bool IsExistingRegistration<TFactory>(ServiceDescriptor descriptor) where TFactory : ClientFactoryBase =>
            !descriptor.IsKeyedService
            && descriptor.ImplementationFactory?.Target is TFactory;

        RemoveLast(services, IsExistingRegistration<ClientFactory>);
        RemoveLast(services, IsExistingRegistration<ClientSnapshotFactory>);

#pragma warning disable IDE0001 // Simplify Names
        services.AddSingleton<IConfigCatClient>(new ClientFactory(clientName: Options.DefaultName).CreateDefault);
        services.AddScoped<IConfigCatClientSnapshot>(new ClientSnapshotFactory(clientName: Options.DefaultName).CreateDefault);

        services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(configureOptions);
#pragma warning restore IDE0001 // Simplify Names
    }

    private static void RegisterNamedClient(IServiceCollection services, string clientName, ConfigureClientOptions configureOptions)
    {
        bool IsExistingRegistration<TFactory>(ServiceDescriptor descriptor) where TFactory : ClientFactoryBase =>
            descriptor.IsKeyedService
            && descriptor.KeyedImplementationFactory?.Target is TFactory factory
            && factory.ClientName == clientName;

        RemoveLast(services, IsExistingRegistration<ClientFactory>);
        RemoveLast(services, IsExistingRegistration<ClientSnapshotFactory>);

#pragma warning disable IDE0001 // Simplify Names
        services.AddKeyedSingleton<IConfigCatClient>(clientName, new ClientFactory(clientName).CreateNamed);
        services.AddKeyedScoped<IConfigCatClientSnapshot>(clientName, new ClientSnapshotFactory(clientName).CreateNamed);

        services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(configureOptions);
#pragma warning restore IDE0001 // Simplify Names
    }

    private static void ConfigureInitializerOptions(IServiceCollection services, ConfigCatInitMode initMode, bool throwOnInitFailure)
    {
        services.Configure<ConfigCatInitializerOptions>(options =>
        {
            options.Mode = initMode;
            options.ThrowOnFailure = throwOnInitFailure;
        });
    }

    private static void RemoveLast(IServiceCollection services, Func<ServiceDescriptor, bool> match)
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

    internal static IEnumerable<string> GetClientNamesFrom(IServiceProvider services)
    {
        // NOTE: Currently there is no way to resolve keys for some keyed service from a service provider
        // (see also https://github.com/dotnet/runtime/issues/100105), we resort to obtaining those
        // from IConfigureOptions registrations.

        return services.GetRequiredService<IEnumerable<IConfigureOptions<ExtendedConfigCatClientOptions>>>()
            .OfType<ConfigureClientOptions>()
            .Select(configurer => configurer.Name!)
            .Distinct();
    }

    private abstract class ClientFactoryBase(string clientName)
    {
        public readonly string ClientName = clientName;
    }

    private sealed class ClientFactory(string clientName) : ClientFactoryBase(clientName)
    {
#pragma warning disable CA1822 // Member '{0}' does not access instance data and can be marked as static
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
#pragma warning restore CA1822 // Member '{0}' does not access instance data and can be marked as static
    }

    private sealed class ClientSnapshotFactory(string clientName) : ClientFactoryBase(clientName)
    {
#pragma warning disable CA1822 // Member '{0}' does not access instance data and can be marked as static
        public IConfigCatClientSnapshot CreateDefault(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IConfigCatClient>().Snapshot();
        }

        public IConfigCatClientSnapshot CreateNamed(IServiceProvider serviceProvider, object? name)
        {
            Debug.Assert(Equals(name, this.ClientName));
            return serviceProvider.GetRequiredKeyedService<IConfigCatClient>(this.ClientName).Snapshot();
        }
#pragma warning restore CA1822 // Member '{0}' does not access instance data and can be marked as static
    }

    private sealed class ConfigureClientOptions : ConfigureNamedOptions<ExtendedConfigCatClientOptions>
    {
        private readonly IConfiguration? configuration;

        public ConfigureClientOptions(string name, IConfiguration configuration)
            : base(name, action: null)
        {
            this.configuration = configuration;
        }

        public ConfigureClientOptions(string name, Action<ExtendedConfigCatClientOptions> action)
            : base(name, action) { }

        public override void Configure(string? name, ExtendedConfigCatClientOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (name == Name)
            {
                if (this.configuration is not null)
                {
                    // In .NET 8+ builds, configuration binding is source generated (see also csproj).
                    this.configuration.Bind(new ExtendedConfigCatClientOptions.BindingWrapper(options));
                }

                Action?.Invoke(options);
            }
        }
    }

    private sealed class ConfigureCommonClientOptions(ILogger<ConfigCatClient> logger)
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
