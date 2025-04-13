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
    private static void ConfigureDefaultOptions(IServiceCollection services)
    {
        services.TryAddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(sp =>
        {
            var logger = sp.GetService<ILogger<ConfigCatClient>>();

            return logger is not null
                ? new ConfigureNamedOptions<ExtendedConfigCatClientOptions, ILogger<ConfigCatClient>>(
                    name: null, // null means that it applies to all named and unnamed options
                    logger,
                    (options, logger) => options.Logger = new ConfigCatToMSLoggerAdapter(logger))
                : new ConfigureOptions<ExtendedConfigCatClientOptions>(action: null);
        });
    }

    private readonly ConfigCatInitStrategy initStrategy;
    private readonly Action<string, ClientRegistration>? registerClientImmediately;

    private Dictionary<string, ClientRegistration>? clientRegistrations;

    public ConfigCatBuilder(IHostBuilder hostBuilder, ConfigCatInitStrategy initStrategy)
    {
        this.initStrategy = initStrategy;

        hostBuilder.ConfigureServices((context, services) =>
        {
            ConfigureDefaultOptions(services);

            if (this.clientRegistrations is { Count: > 0 })
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

                if (this.initStrategy != ConfigCatInitStrategy.DoNotWaitForClientReady)
                {
                    services.AddHostedService(sp => new ConfigCatInitService(sp, this.clientRegistrations.Keys, this.initStrategy));
                }
            }
        });
    }

    public ConfigCatBuilder(IHostApplicationBuilder hostApplicationBuilder, ConfigCatInitStrategy initStrategy)
    {
        this.initStrategy = initStrategy;

        var configuration = hostApplicationBuilder.Configuration;
        var services = hostApplicationBuilder.Services;

        ConfigureDefaultOptions(services);

        this.registerClientImmediately = (clientKey, clientRegistration) =>
        {
            if (clientKey == Options.DefaultName)
            {
                if (this.clientRegistrations is not null && this.clientRegistrations.ContainsKey(clientKey))
                {
                    ClientRegistration.UnregisterDefault(services);
                }
                clientRegistration.RegisterDefault(services, configuration);
            }
            else
            {
                if (this.clientRegistrations is not null && this.clientRegistrations.ContainsKey(clientKey))
                {
                    ClientRegistration.UnregisterKeyed(clientKey, services);
                }

                clientRegistration.RegisterKeyed(clientKey, services, configuration);
            }

            if (this.clientRegistrations is null && this.initStrategy != ConfigCatInitStrategy.DoNotWaitForClientReady)
            {
                this.clientRegistrations = new();
                services.AddHostedService(sp => new ConfigCatInitService(sp, this.clientRegistrations.Keys, this.initStrategy));
            }
        };
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
        this.registerClientImmediately?.Invoke(clientKey, clientRegistration);
        (this.clientRegistrations ??= new())[clientKey] = clientRegistration;

        return this;
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
