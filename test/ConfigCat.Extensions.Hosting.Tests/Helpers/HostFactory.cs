using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConfigCat.Extensions.Hosting.Tests.Helpers;

internal static class HostFactory
{
    private static void SetupLogging(ILoggingBuilder builder)
    {
        // NOTE: Some of the test target frameworks don't support all the logger providers added by the host builder
        // by default, so we need to make sure that those are excluded.

        builder
            .ClearProviders()
            .AddDebug();
    }

    public static HostApplicationBuilder CreateMinimalHostBuilder()
    {
        var builder = Host.CreateApplicationBuilder();
        SetupLogging(builder.Logging);
        return builder;
    }

    public static IHostBuilder CreateLegacyHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureLogging(SetupLogging);
        return builder;
    }
}
