using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;
using ConfigCat.Extensions.Hosting.Adapters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigCat.Extensions.Hosting;

public sealed class ConfigCatBuilder
{
    private const ConfigCatInitStrategy UnsetInitStrategy = (ConfigCatInitStrategy)(-1);
    private const ConfigCatInitStrategy DefaultInitStrategy = ConfigCatInitStrategy.DoNotWaitForClientReady;

    private static void ConfigureDefaultOptions(IServiceCollection services)
    {
        services.TryAddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(sp =>
        {
            var logger = sp.GetService<ILogger<ConfigCatClient>>();

            return logger is not null
                ? new ConfigureNamedOptions<ExtendedConfigCatClientOptions, ILogger<ConfigCatClient>>(
                    name: null, // null means configuring all options (named or unnamed)
                    logger,
                    (options, logger) => options.Logger = new ConfigCatToMSLoggerAdapter(logger))
                : new ConfigureOptions<ExtendedConfigCatClientOptions>(action: null);
        });
    }

    private readonly IConfiguration? configuration;
    private readonly IServiceCollection? services;
    private ConfigCatInitStrategy initStrategy = UnsetInitStrategy;
    private readonly Dictionary<string, ClientRegistration> clientRegistrations = new();

    internal ConfigCatBuilder() { }

    internal ConfigCatBuilder(IHostApplicationBuilder hostApplicationBuilder)
    {
        this.configuration = hostApplicationBuilder.Configuration;
        this.services = hostApplicationBuilder.Services;

        ConfigureDefaultOptions(this.services);
    }

    private void RegisterInitServiceIfNecessary(IServiceCollection services)
    {
        Debug.Assert(this.initStrategy != UnsetInitStrategy);

        if (this.initStrategy != ConfigCatInitStrategy.DoNotWaitForClientReady)
        {
            services.AddHostedService(sp => new ConfigCatInitService(sp, this.clientRegistrations.Keys, this.initStrategy));
        }
    }

    public ConfigCatBuilder UseInitStrategy(ConfigCatInitStrategy initStrategy)
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

    private void RegisterClientImmediately(string clientKey, ClientRegistration clientRegistration)
    {
        Debug.Assert(this.configuration is not null && this.services is not null);

        var configuration = this.configuration!;
        var services = this.services!;

        if (clientKey == Options.DefaultName)
        {
            if (this.clientRegistrations.ContainsKey(clientKey))
            {
                ClientRegistration.UnregisterDefault(services);
            }

            clientRegistration.RegisterDefault(services, configuration);
        }
        else
        {
            if (this.clientRegistrations.ContainsKey(clientKey))
            {
                ClientRegistration.UnregisterKeyed(clientKey, services);
            }

            clientRegistration.RegisterKeyed(clientKey, services, configuration);
        }

        if (this.initStrategy == UnsetInitStrategy)
        {
            this.initStrategy = DefaultInitStrategy;

            RegisterInitServiceIfNecessary(services);
        }
    }

    public ConfigCatBuilder AddDefaultClient(Action<ExtendedConfigCatClientOptions>? configureOptions = null)
    {
        return AddClient(clientKey: Options.DefaultName, configureOptions);
    }

    public ConfigCatBuilder AddKeyedClient(string clientKey, Action<ExtendedConfigCatClientOptions>? configureOptions = null)
    {
        if (clientKey is null)
        {
            throw new ArgumentNullException(nameof(clientKey));
        }

        if (clientKey == Options.DefaultName)
        {
            throw new ArgumentException($"Client key cannot be equal to \"{Options.DefaultName}\".", nameof(clientKey));
        }

        return AddClient(clientKey, configureOptions);
    }

    private ConfigCatBuilder AddClient(string clientKey, Action<ExtendedConfigCatClientOptions>? configureOptions = null)
    {
        var clientRegistration = new ClientRegistration(configureOptions);
        if (this.services is not null)
        {
            RegisterClientImmediately(clientKey, clientRegistration);
        }
        this.clientRegistrations[clientKey] = clientRegistration;

        return this;
    }

    internal IServiceCollection Build(IServiceCollection services, HostBuilderContext context)
    {
        ConfigureDefaultOptions(services);

        if (this.clientRegistrations.Count > 0)
        {
            foreach (var kvp in this.clientRegistrations)
            {
                if (kvp.Key == Options.DefaultName)
                {
                    kvp.Value.RegisterDefault(services, context.Configuration);
                }
                else
                {
                    kvp.Value.RegisterKeyed(kvp.Key, services, context.Configuration);
                }
            }

            if (this.initStrategy == UnsetInitStrategy)
            {
                this.initStrategy = DefaultInitStrategy;
            }

            RegisterInitServiceIfNecessary(services);
        }

        return services;
    }

    private static void RemoveLast(IServiceCollection services, Func<ServiceDescriptor, bool> predicate)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (predicate(services[i]))
            {
                services.RemoveAt(i);
                break;
            }
        }
    }

    private readonly struct ClientRegistration(Action<ExtendedConfigCatClientOptions>? configureOptions)
    {
        public void RegisterDefault(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConfigCatClient>(new ClientFactory(clientKey: Options.DefaultName).CreateDefault);

            var section = configuration.GetSection("ConfigCat").GetSection("DefaultClient");
            var configureClientOptions = new ConfigureClientOptions(
                name: Options.DefaultName,
                configuration: section.Exists() ? section : null,
                userAction: configureOptions,
                combinedAction: (options, configuration, userAction) =>
                {
                    if (configuration is not null)
                    {
                        // In .NET 8+ builds configuration binding is source generated (see also csproj).
                        configuration.Bind(new ExtendedConfigCatClientOptions.BindingWrapper(options));
                    }

                    userAction?.Invoke(options);
                });

            services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(configureClientOptions);
        }

        public static void UnregisterDefault(IServiceCollection services)
        {
            RemoveLast(services, descriptor => descriptor.ImplementationInstance is ConfigureClientOptions configureOptions && configureOptions.Name == Options.DefaultName);
            RemoveLast(services, descriptor => descriptor.ImplementationFactory?.Target is ClientFactory);
        }

        public void RegisterKeyed(string clientKey, IServiceCollection services, IConfiguration configuration)
        {
            services.AddKeyedSingleton<IConfigCatClient>(clientKey, new ClientFactory(clientKey).CreateKeyed);

            var section = configuration.GetSection("ConfigCat").GetSection("NamedClients").GetSection(clientKey);
            var configureClientOptions = new ConfigureClientOptions(
                name: clientKey,
                configuration: section.Exists() ? section : null,
                userAction: configureOptions,
                combinedAction: (options, configuration, userAction) =>
                {
                    if (configuration is not null)
                    {
                        // In .NET 8+ builds configuration binding is source generated (see also csproj).
                        configuration.Bind(new ExtendedConfigCatClientOptions.BindingWrapper(options));
                    }

                    userAction?.Invoke(options);
                });

            services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(configureClientOptions);
        }

        public static void UnregisterKeyed(string clientKey, IServiceCollection services)
        {
            RemoveLast(services, descriptor => descriptor.ImplementationInstance is ConfigureClientOptions configureOptions && configureOptions.Name == clientKey);
            RemoveLast(services, descriptor => descriptor.KeyedImplementationFactory?.Target is ClientFactory factory && factory.ClientKey == clientKey);
        }
    }

    private sealed class ClientFactory(string clientKey)
    {
        public readonly string ClientKey = clientKey;

#pragma warning disable CA1822 // Member 'CreateDefault' does not access instance data and can be marked as static
        public IConfigCatClient CreateDefault(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExtendedConfigCatClientOptions>>().Value;
            return ConfigCatClient.Get(options.SdkKey!, options);
        }

        public IConfigCatClient CreateKeyed(IServiceProvider serviceProvider, object? key)
        {
            Debug.Assert(Equals(key, this.ClientKey));
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<ExtendedConfigCatClientOptions>>().Get(name: this.ClientKey);
            return ConfigCatClient.Get(options.SdkKey!, options);
        }
#pragma warning restore CA1822
    }

    private sealed class ConfigureClientOptions(
        string? name,
        IConfiguration? configuration,
        Action<ExtendedConfigCatClientOptions>? userAction,
        Action<ExtendedConfigCatClientOptions, IConfiguration, Action<ExtendedConfigCatClientOptions>>? combinedAction)
        : ConfigureNamedOptions<ExtendedConfigCatClientOptions, IConfiguration, Action<ExtendedConfigCatClientOptions>>(
            name, configuration!, userAction!, combinedAction)
    {
    }
}
