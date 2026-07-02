using BlazorWasm;
using ConfigCat.Client;
using ConfigCat.Extensions.Hosting;
using ConfigCat.Extensions.Hosting.Configuration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddConfigCat(builder.Configuration, configCatBuilder => configCatBuilder
    // Register the ConfigCat client as a singleton service so you can inject it in your components.
    .AddDefaultClient(options =>
    {
        options.SdkKey = "configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/tiOvFw5gkky9LFu1Duuvzw";
        options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(5));
    })
    .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: false)));

// In browser applications, it's recommended to configure the ConfigCat client to use the browser's
// localStorage API for caching config data to reduce ConfigCat network traffic. However, this is a
// bit tricky in Blazor because we need to get the IJSRuntime service from the DI container to configure
// the client options. This can be achieved using a custom IConfigureOptions<ExtendedConfigCatClientOptions>
// implementation:
var configureClientToUseLocalStorage = new LocalStorageConfigCache.ConfigureClientOptions();
builder.Services.AddSingleton<IConfigureOptions<ExtendedConfigCatClientOptions>>(configureClientToUseLocalStorage);

builder.Logging.SetMinimumLevel(builder.HostEnvironment.IsDevelopment()
    ? Microsoft.Extensions.Logging.LogLevel.Information
    : Microsoft.Extensions.Logging.LogLevel.Warning);

var host = builder.Build();

// Since JS interop is asynchronous, we need to take this extra initialization step before
// the ConfigCat client is resolved from the DI container.
await configureClientToUseLocalStorage.InitializeAsync(host.Services);

// Usually, it's a good idea to ensure that the ConfigCat client is initialized before
// the application starts, especially if you want to use synchronous feature flag evaluation.
await host.Services.GetRequiredService<IConfigCatInitializer>().InitializeAsync();

await host.RunAsync();
