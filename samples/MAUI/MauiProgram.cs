using ConfigCat.Client;
using ConfigCat.Client.Extensions.Adapters;
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

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
#endif

        // Register ConfigCatClient as a singleton service so you can inject it in your view models.
        builder.Services.AddSingleton<IConfigCatClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ConfigCatClient>>();

            return ConfigCatClient.Get("PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ", options =>
            {
                options.PollingMode = PollingModes.AutoPoll();
                options.Logger = new ConfigCatToMSLoggerAdapter(logger);
            });
        });

        return builder.Build();
    }
}
