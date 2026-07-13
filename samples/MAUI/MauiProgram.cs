using ConfigCat.Client;
using ConfigCat.Extensions.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MauiSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddScoped<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
#endif

        builder.UseConfigCat()
            // Register the ConfigCat client as a singleton service so you can inject it in your components.
            .AddDefaultClient(options =>
            {
                options.SdkKey = "configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/tiOvFw5gkky9LFu1Duuvzw";
                options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(5));
            })
            .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: false));

        var app = builder.Build();

        // Usually, it's a good idea to ensure that the ConfigCat client is initialized before
        // the application starts, especially if you want to use synchronous feature flag evaluation.
        // (Please note that this block-waiting code is for demonstration only. In real-world apps,
        // you should perform this operation asynchronously while displaying a splash screen.)
        app.Services.GetRequiredService<IConfigCatInitializer>().InitializeAsync().GetAwaiter().GetResult();

        return app;
    }
}
