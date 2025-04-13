using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;
using ConfigCat.Extensions.Hosting.Adapters;
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
        services.TryAddSingleton<IConfigureOptions<ConfigCatClientOptions>>(sp =>
        {
            var logger = sp.GetService<ILogger<ConfigCatClient>>();

            return logger is not null
                ? new ConfigureNamedOptions<ConfigCatClientOptions, ILogger<ConfigCatClient>>(
                    name: null, // null means that it applies to all named and unnamed options
                    logger,
                    (options, logger) => options.Logger = new ConfigCatToMSLoggerAdapter(logger))
                : new ConfigureOptions<ConfigCatClientOptions>(action: null);
        });
    }

    private readonly ConfigCatInitStrategy initStrategy;
    private readonly Action<string, ClientRegistration>? registerClientImmediately;

    private Dictionary<string, ClientRegistration>? clientRegistrations;

    public ConfigCatBuilder(IHostBuilder hostBuilder, ConfigCatInitStrategy initStrategy)
    {
        this.initStrategy = initStrategy;

        hostBuilder.ConfigureServices((_, services) =>
        {
            ConfigureDefaultOptions(services);

            if (this.clientRegistrations is { Count: > 0 })
            {
                foreach (var kvp in this.clientRegistrations)
                {
                    if (kvp.Key == Options.DefaultName)
                    {
                        kvp.Value.RegisterDefault(services);
                    }
                    else
                    {
                        kvp.Value.RegisterKeyed(kvp.Key, services);
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
                clientRegistration.RegisterDefault(services);
            }
            else
            {
                if (this.clientRegistrations is not null && this.clientRegistrations.ContainsKey(clientKey))
                {
                    ClientRegistration.UnregisterKeyed(clientKey, services);
                }

                clientRegistration.RegisterKeyed(clientKey, services);
            }

            if (this.clientRegistrations is null && this.initStrategy != ConfigCatInitStrategy.DoNotWaitForClientReady)
            {
                this.clientRegistrations = new();
                services.AddHostedService(sp => new ConfigCatInitService(sp, this.clientRegistrations.Keys, this.initStrategy));
            }
        };
    }

    public ConfigCatBuilder AddDefaultClient(string sdkKey, Action<ConfigCatClientOptions>? configureOptions = null)
    {
        return AddClient(clientKey: Options.DefaultName, sdkKey, configureOptions);
    }

    public ConfigCatBuilder AddKeyedClient(string clientKey, string sdkKey, Action<ConfigCatClientOptions>? configureOptions = null)
    {
        if (clientKey is null)
        {
            throw new ArgumentNullException(nameof(clientKey));
        }

        if (clientKey == Options.DefaultName)
        {
            throw new ArgumentException($"Client key cannot be equal to \"{Options.DefaultName}\".", nameof(clientKey));
        }

        return AddClient(clientKey, sdkKey, configureOptions);
    }

    private ConfigCatBuilder AddClient(string clientKey, string sdkKey, Action<ConfigCatClientOptions>? configureOptions = null)
    {
        ConfigCatClient.EnsureNonEmptySdkKey(sdkKey);

        var clientRegistration = new ClientRegistration(sdkKey, configureOptions);
        this.registerClientImmediately?.Invoke(clientKey, clientRegistration);
        (this.clientRegistrations ??= new())[clientKey] = clientRegistration;

        return this;
    }

    private readonly struct ClientRegistration(string sdkKey, Action<ConfigCatClientOptions>? configureOptions)
    {
        public void RegisterDefault(IServiceCollection services)
        {
            services.AddSingleton<IConfigCatClient>(new ClientFactory(clientKey: Options.DefaultName, sdkKey).CreateDefault);

            if (configureOptions is not null)
            {
                services.AddSingleton<IConfigureOptions<ConfigCatClientOptions>>(new ConfigureClientOptions(Options.DefaultName, configureOptions));
            }
        }

        public static void UnregisterDefault(IServiceCollection services)
        {
            RemoveLast(services, descriptor => descriptor.ImplementationInstance is ConfigureClientOptions configureOptions && configureOptions.Name == Options.DefaultName);
            RemoveLast(services, descriptor => descriptor.ImplementationFactory?.Target is ClientFactory);
        }

        public void RegisterKeyed(string clientKey, IServiceCollection services)
        {
            services.AddKeyedSingleton<IConfigCatClient>(clientKey, new ClientFactory(clientKey, sdkKey).CreateKeyed);

            if (configureOptions is not null)
            {
                services.AddSingleton<IConfigureOptions<ConfigCatClientOptions>>(new ConfigureClientOptions(name: clientKey, configureOptions));
            }
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

    private sealed class ClientFactory(string clientKey, string sdkKey)
    {
        public readonly string ClientKey = clientKey;

        public IConfigCatClient CreateDefault(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<ConfigCatClientOptions>>().Value;
            return ConfigCatClient.Get(sdkKey, options, reportInstanceAlreadyCreated: false);
        }

        public IConfigCatClient CreateKeyed(IServiceProvider serviceProvider, object? key)
        {
            Debug.Assert(Equals(key, this.ClientKey));
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<ConfigCatClientOptions>>().Get(name: this.ClientKey);
            return ConfigCatClient.Get(sdkKey, options, reportInstanceAlreadyCreated: false);
        }
    }

    private sealed class ConfigureClientOptions : ConfigureNamedOptions<ConfigCatClientOptions>
    {
        public ConfigureClientOptions(string? name, Action<ConfigCatClientOptions>? action) : base(name, action) { }
    }
}
