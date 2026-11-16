using System;
using ConfigCat.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Configuring the DI container

var services = new ServiceCollection()
    .AddConfigCat(builder =>
    {
        // Registering the default ConfigCat client service using the SDK Key
        builder.AddDefaultClient(options =>
        {
            options.SdkKey = "configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/tiOvFw5gkky9LFu1Duuvzw";
        });
    })
    .AddLogging(builder =>
    {
        // Setting log level to Information to show detailed feature flag evaluation
        builder.AddFilter("ConfigCat.Client", Microsoft.Extensions.Logging.LogLevel.Information);
        builder.AddConsole();
    });

// Building the DI container

await using var serviceProvider = services.BuildServiceProvider();

// Resolving the ConfigCat client service
// (no manual disposal needed, the DI container handles it)

var client = serviceProvider.GetRequiredService<IConfigCatClient>();

// Creating a user object to identify the user (optional)
var user = new User("<SOME USERID>")
{
    Country = "US",
    Email = "configcat@example.com",
    Custom =
    {
        { "SubscriptionType", "Pro"},
        { "Role", "Admin"},
        { "version", "1.0.0" }
    }
};

// Accessing feature flag or setting value
var value = await client.GetValueAsync("isPOCFeatureEnabled", false, user);
Console.WriteLine($"isPOCFeatureEnabled: {value}");
