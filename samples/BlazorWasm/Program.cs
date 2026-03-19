using BlazorWasm;
using ConfigCat.Client;
using ConfigCat.HostingIntegration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Logging.SetMinimumLevel(builder.HostEnvironment.IsDevelopment()
    ? Microsoft.Extensions.Logging.LogLevel.Information
    : Microsoft.Extensions.Logging.LogLevel.Warning);

builder.Services.AddConfigCat(builder.Configuration, configCatBuilder => configCatBuilder
    // Register the ConfigCat client as a singleton service so you can inject it in your components.
    .AddDefaultClient(options =>
    {
        options.SdkKey = "PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ";
        options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(5));
    }));

var host = builder.Build();

// Usually, it's a good idea to ensure that the ConfigCat client is initialized before
// the application starts, especially if you want to use synchronous feature flag evaluation.
await host.Services.GetRequiredService<IConfigCatInitializer>()
    .InitializeAsync(ConfigCatInitArgs.From(builder.Services, ConfigCatInitStrategy.WaitForClientReadyAndLogOnFailure));

await host.RunAsync();
