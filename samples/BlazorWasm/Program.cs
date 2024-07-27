using BlazorWasm;
using ConfigCat.Client;
using ConfigCat.Client.Extensions.Adapters;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Logging.SetMinimumLevel(builder.HostEnvironment.IsDevelopment()
    ? Microsoft.Extensions.Logging.LogLevel.Information
    : Microsoft.Extensions.Logging.LogLevel.Warning);

// Register ConfigCatClient as a singleton service so you can inject it in your components.
builder.Services.AddSingleton<IConfigCatClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ConfigCatClient>>();

    return ConfigCatClient.Get("PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ", options =>
    {
        options.PollingMode = PollingModes.AutoPoll();
        options.Logger = new ConfigCatToMSLoggerAdapter(logger);
    });
});

await builder.Build().RunAsync();
