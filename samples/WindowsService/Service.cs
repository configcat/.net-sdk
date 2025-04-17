using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WindowsService;

internal class Service : BackgroundService
{
    private readonly IConfigCatClient configCatClient;
    private readonly ILogger<Service> logger;
    private readonly object syncObj = new();
    private IConfigCatClientSnapshot? latestSnapshot;

    public Service(IConfigCatClient configCatClient, ILogger<Service> logger)
    {
        this.configCatClient = configCatClient;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EventHandler<ConfigChangedEventArgs> configChangedHandler = (s, e) =>
        {
            lock (this.syncObj)
            {
                OnConfigChanged((IConfigCatClient)s!);
            }
        };

        this.configCatClient.ConfigChanged += configChangedHandler;
        try
        {
            lock (this.syncObj)
            {
                if (this.latestSnapshot is null)
                {
                    OnConfigChanged(this.configCatClient);
                }
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            this.configCatClient.ConfigChanged -= configChangedHandler;
        }
    }

    private void OnConfigChanged(IConfigCatClient configCatClient)
    {
        var snapshot = configCatClient.Snapshot();

        this.latestSnapshot = snapshot;

        if (snapshot.CacheState != ClientCacheState.NoFlagData)
        {
            var value = snapshot.GetValue<bool?>("isAwesomeFeatureEnabled", null);
            this.logger.LogInformation("isAwesomeFeatureEnabled: {VALUE}", value);
        }
    }
}
