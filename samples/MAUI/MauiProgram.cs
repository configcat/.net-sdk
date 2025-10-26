using ConfigCat.Client;
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

        builder.Services.AddConfigCat(builder.Configuration, configCatBuilder => configCatBuilder
            // Register ConfigCatClient as a singleton service so you can inject it in your components.
            .AddDefaultClient(options =>
            {
                options.SdkKey = "PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ";
                options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(5));
            }));

        return builder.Build();
    }
}
