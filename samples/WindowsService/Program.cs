using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=hostbuilder

namespace WindowsService;

public static class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            /* Calling `ConfigureConfigCat()` on the host builder is all you need to register ConfigCat clients
               based on application configuration (see appsettings.json). */
            .ConfigureConfigCat(builder =>
            {
                /* You can also configure ConfigCat clients by code. Please note that if you add a client here that
                   is already defined in the configuration (e.g. appsettings.json), the settings from the configuration
                   and the settings made by code will be merged, with the latter taking precedence. */

                //builder
                //    .AddDefaultClient(options =>
                //    {
                //        options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(10));
                //    })
                //    .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: true));
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
