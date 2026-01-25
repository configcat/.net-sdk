using System;
using ConfigCat.Client;
using ConfigCat.HostingIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=hostbuilder

namespace WindowsService;

public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureConfigCat(builder =>
            {
                builder
                    // Uncomment the following lines if you want to configure ConfigCat clients by code. Please note that
                    // if you add a client here that is already defined in the configuration, the settings from the
                    // configuration and the settings made by code will be merged, with the latter taking precedence.
                    //.AddDefaultClient(options =>
                    //{
                    //    options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(10));
                    //})
                    .UseInitStrategy(ConfigCatInitStrategy.WaitForClientReadyAndLogOnFailure);
            })
            .ConfigureLogging((context, builder) =>
            {
                builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                builder.ClearProviders();
                builder.AddFile(options => options.RootPath = context.HostingEnvironment.ContentRootPath);
                if (context.HostingEnvironment.IsDevelopment())
                {
                    builder.AddConsole();
                }
            })
            .ConfigureServices(services =>
            {
                services.AddHostedService<Service>();
            })
            .UseWindowsService()
            .Build();

        host.Run();
    }
}
