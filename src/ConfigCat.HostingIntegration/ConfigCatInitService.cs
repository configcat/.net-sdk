using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Client.ConfigService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigCat.HostingIntegration;

internal sealed class ConfigCatInitService(IServiceProvider serviceProvider, IReadOnlyCollection<string> clientKeys, ConfigCatInitStrategy initStrategy) : IHostedLifecycleService
{
    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        var logger = serviceProvider.GetService<ILogger<ConfigCatInitService>>();

        var keyedServiceProvider = serviceProvider as IKeyedServiceProvider;

        static IKeyedServiceProvider EnsureKeyedServiceProvider(IKeyedServiceProvider? keyedServiceProvider, IServiceProvider serviceProvider)
        {
            return keyedServiceProvider
                ?? throw new InvalidOperationException($"The configured service provider {serviceProvider.GetType()} does not support keyed services.");
        }

        var clients = new Dictionary<IConfigCatClient, List<string>>();
        foreach (var clientKey in clientKeys)
        {
            var client = clientKey == Options.DefaultName
                ? serviceProvider.GetRequiredService<IConfigCatClient>()
                : EnsureKeyedServiceProvider(keyedServiceProvider, serviceProvider).GetRequiredKeyedService<IConfigCatClient>(clientKey);

            if (!clients.TryGetValue(client, out var clientKeyList))
            {
                clients.Add(client, clientKeyList = new(capacity: 1));
            }

            clientKeyList.Add(clientKey);
        }

        clientKeys = null!; // NOTE: Not needed any more, let GC clean it up.

        logger?.LogInformation("Waiting for {CLIENT_COUNT} ConfigCat client instance(s) to initalize...", clients.Count);

        var cacheStates = await Task.WhenAll(clients.Keys.Select(client => client.WaitForReadyAsync(cancellationToken)));

        if (logger is not null || initStrategy == ConfigCatInitStrategy.WaitForClientReadyAndThrowOnFailure)
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
                logger?.LogInformation("All ConfigCat client instances are initialized and ready to evaluate feature flags.");
            }
            else
            {
                const string messageFormat = "One or more ConfigCat client instances failed to initialize within maxInitWaitTime: {0}.";
                // TODO: redact SDK Keys
                var clientKeys = string.Join(", ", uninitalizedClients
                    .SelectMany(client => clients[client])
                    .Select(clientKey => clientKey == Options.DefaultName ? "(default)" : "'" + clientKey + "'"));

                if (initStrategy == ConfigCatInitStrategy.WaitForClientReadyAndThrowOnFailure)
                {
                    throw new TimeoutException(string.Format(messageFormat, clientKeys));
                }
                else
                {
                    var message = string.Format(messageFormat, "{CLIENT_KEYS}") + " They may still be able to initialize later.";
                    logger?.LogWarning(message, clientKeys);
                }
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
