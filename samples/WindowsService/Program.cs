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
                    .AddDefaultClient(options =>
                    {
                        // Settings made in this callback take precendence over the settings coming from configuration
                        // (environment variables, appsettings.json, etc.)

                        //options.SdkKey = "PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ";
                        //options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(5));
                    })
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
