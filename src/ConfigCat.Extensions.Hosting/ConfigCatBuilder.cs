using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConfigCat.Client;
using ConfigCat.Extensions.Hosting.Adapters;
using ConfigCat.Extensions.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigCat.Extensions.Hosting;

public abstract class ConfigCatBuilder<TBuilder> where TBuilder : ConfigCatBuilder<TBuilder>
{
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

    private protected readonly IServiceCollection? services; // set for minimal hosting model only
    private protected readonly IConfiguration? configuration; // set for minimal hosting model only
    private readonly Dictionary<string, ClientRegistration> clientRegistrations = new();

    private protected ConfigCatBuilder() { } // for legacy hosting model

    private protected ConfigCatBuilder(IServiceCollection services, IConfiguration? configuration) // for minimal hosting model
    {
        this.services = services;
        this.configuration = configuration;

        ConfigureDefaultOptions(this.services);
    }

    private protected IReadOnlyCollection<string> ClientKeys => this.clientRegistrations.Keys;

    private void RegisterClientImmediately(string clientKey, ClientRegistration clientRegistration)
    {
        Debug.Assert(this.configuration is not null && this.services is not null);

        var configuration = this.configuration;
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

        OnClientRegistered(services);
    }

    public TBuilder AddDefaultClient(Action<ExtendedConfigCatClientOptions>? configureOptions = null)
    {
        return AddClient(clientKey: Options.DefaultName, configureOptions);
    }

    public TBuilder AddKeyedClient(string clientKey, Action<ExtendedConfigCatClientOptions>? configureOptions = null)
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

    private TBuilder AddClient(string clientKey, Action<ExtendedConfigCatClientOptions>? configureOptions = null)
    {
        var clientRegistration = new ClientRegistration(configureOptions);
        if (this.services is not null)
        {
            RegisterClientImmediately(clientKey, clientRegistration);
        }
        this.clientRegistrations[clientKey] = clientRegistration;

        return (TBuilder)this;
    }

    internal IServiceCollection Build(IServiceCollection services, IConfiguration? configuration)
    {
        ConfigureDefaultOptions(services);

        if (this.clientRegistrations.Count > 0)
        {
            foreach (var kvp in this.clientRegistrations)
            {
                if (kvp.Key == Options.DefaultName)
                {
                    kvp.Value.RegisterDefault(services, configuration);
                }
                else
                {
                    kvp.Value.RegisterKeyed(kvp.Key, services, configuration);
                }
            }

            OnClientRegistered(services);
        }

        return services;
    }

    private protected virtual void OnClientRegistered(IServiceCollection services) { }

    private protected static void RemoveLast(IServiceCollection services, Func<ServiceDescriptor, bool> predicate)
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
        public void RegisterDefault(IServiceCollection services, IConfiguration? configuration)
        {
            services.AddSingleton<IConfigCatClient>(new ClientFactory(clientKey: Options.DefaultName).CreateDefault);

            var section = configuration?.GetSection("ConfigCat").GetSection("DefaultClient");
            var configureClientOptions = new ConfigureClientOptions(
                name: Options.DefaultName,
                configuration: section is not null && section.Exists() ? section : null,
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

        public void RegisterKeyed(string clientKey, IServiceCollection services, IConfiguration? configuration)
        {
            services.AddKeyedSingleton<IConfigCatClient>(clientKey, new ClientFactory(clientKey).CreateKeyed);

            var section = configuration?.GetSection("ConfigCat").GetSection("NamedClients").GetSection(clientKey);
            var configureClientOptions = new ConfigureClientOptions(
                name: clientKey,
                configuration: section is not null && section.Exists() ? section : null,
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

public sealed class ConfigCatBuilder : ConfigCatBuilder<ConfigCatBuilder>
{
    internal ConfigCatBuilder() { }
}
