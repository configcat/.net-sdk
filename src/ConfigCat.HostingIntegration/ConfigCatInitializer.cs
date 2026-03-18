using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Client.ConfigService;
using ConfigCat.HostingIntegration.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigCat.HostingIntegration;

internal sealed class ConfigCatInitializer : IConfigCatInitializer
{
    private readonly IServiceProvider serviceProvider;

    private readonly ConfigCatInitStrategy initStrategy;
    private readonly IReadOnlyCollection<string> clientNames;

    public ConfigCatInitializer(IOptions<ConfigCatInitializerOptions> options, IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        var opts = options.Value;
        this.initStrategy = opts.InitStrategy;
        this.clientNames = opts.GetClientNames() ?? Array.Empty<string>();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (this.initStrategy == ConfigCatInitStrategy.DoNotInitializeClients)
        {
            return;
        }

        var logger = this.serviceProvider.GetService<ILogger<ConfigCatInitializer>>();

        var keyedServiceProvider = this.serviceProvider as IKeyedServiceProvider;

        static IKeyedServiceProvider EnsureKeyedServiceProvider(IKeyedServiceProvider? keyedServiceProvider, IServiceProvider serviceProvider)
        {
            return keyedServiceProvider
                ?? throw new InvalidOperationException($"The configured service provider {serviceProvider.GetType()} does not support keyed services.");
        }

        // Resolve clients

        var clients = new Dictionary<IConfigCatClient, List<string>>();
        foreach (var clientName in this.clientNames)
        {
            var client = clientName == Options.DefaultName
                ? this.serviceProvider.GetRequiredService<IConfigCatClient>()
                : EnsureKeyedServiceProvider(keyedServiceProvider, this.serviceProvider).GetRequiredKeyedService<IConfigCatClient>(clientName);

            if (!clients.TryGetValue(client, out var clientNameList))
            {
                clients.Add(client, clientNameList = new(capacity: 1));
            }

            clientNameList.Add(clientName);
        }

        // If requested, wait for clients to reach the ready state 

        if (this.initStrategy == ConfigCatInitStrategy.DoNotWaitForClientReady)
        {
            return;
        }

        logger?.LogInformation($"Waiting for {{CLIENT_COUNT}} {nameof(ConfigCatClient)} instance(s) to initalize...", clients.Count);

        var cacheStates = await Task.WhenAll(clients.Keys.Select(client => client.WaitForReadyAsync(cancellationToken)));

        // Log or throw on failure

        if (logger is not null || this.initStrategy == ConfigCatInitStrategy.WaitForClientReadyAndThrowOnFailure)
        {
            var uninitalizedClients = clients.Keys
                .Zip(cacheStates, (client, cacheState) => (client, cacheState))
                .Where(item =>
                    item.cacheState == ClientCacheState.NoFlagData
                    && ((ConfigCatClient)item.client).Uses<AutoPollConfigService>())
                .Select(item => item.client)
                .ToArray();

            if (uninitalizedClients.Length == 0)
            {
                logger?.LogInformation($"All {nameof(ConfigCatClient)} instances are initialized and ready to evaluate feature flags.");
            }
            else
            {
                const string messageFormat = $"One or more {nameof(ConfigCatClient)} instances failed to initialize within maxInitWaitTime: {{0}}.";

                var clientNames = string.Join(", ", uninitalizedClients
                    .SelectMany(client => clients[client])
                    .Select(clientName => clientName == Options.DefaultName ? "(default)" : "'" + clientName + "'"));

                if (this.initStrategy == ConfigCatInitStrategy.WaitForClientReadyAndThrowOnFailure)
                {
                    throw new TimeoutException(string.Format(messageFormat, clientNames));
                }
                else
                {
                    var message = string.Format(messageFormat, "{CLIENT_KEYS}") + " They may still be able to initialize later.";
                    logger?.LogWarning(message, clientNames);
                }
            }
        }
    }
}
