using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Extensions.Hosting.Configuration;
using ConfigCat.Extensions.Hosting.Tests.Fakes;
using ConfigCat.Extensions.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Extensions.Hosting.Tests;

[TestClass]
[DoNotParallelize]
public class ConsoleLoggingTests
{
    private static readonly StringBuilder ConsoleOutput = new();

#if NET10_0_OR_GREATER
    // NOTE: Console redirection doesn't seem to work for console logger before .NET 10.

    private static TextWriter? OriginalConsoleOut;

    [TestInitialize]
    public void Init()
    {
        OriginalConsoleOut = Console.Out;
        Console.SetOut(new StringWriter(ConsoleOutput));
    }

    [TestCleanup]
    public void Cleanup()
    {
        Console.SetOut(OriginalConsoleOut!);
    }
#endif

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public async Task LoggerAdapter_StructuredLoggingWorks(HostKind hostKind)
    {
        ConsoleOutput.Clear();

        const string clientName = "my-client";

        IHost host;

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            Action<ExtendedConfigCatClientOptions> configureClient = options =>
            {
                options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                options.FlagOverrides = FlagOverrides.LocalDictionary(new Dictionary<string, object>(), OverrideBehaviour.LocalOnly);
            };

            builder
                .AddDefaultClient(configureClient)
                .AddNamedClient(clientName, configureClient);
        };

        Action<ILoggingBuilder> setupLogging = builder =>
        {
            builder.AddJsonConsole(options =>
            {
                options.JsonWriterOptions = new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            });
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupLogging(appBuilder.Logging);
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureLogging(setupLogging);
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddLogging(setupLogging);
                serviceCollection.AddConfigCat(setupConfigCatBuilder);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using (host)
        {
            var defaultClient = host.Services.GetRequiredKeyedService<IConfigCatClient>(null);
            var namedClient = host.Services.GetRequiredKeyedService<IConfigCatClient>(clientName);

            Assert.IsNull(defaultClient.Snapshot().GetValue("testFlag", (bool?)null));
            Assert.IsNull(namedClient.Snapshot().GetValue("testFlag", (bool?)null));
        }

#if NET10_0_OR_GREATER
        var reader = new StringReader(ConsoleOutput.ToString());
        var logEvents = new List<JsonNode?>();
        while (reader.ReadLine() is { } line)
        {
            logEvents.Add(JsonNode.Parse(line));
        }

        var expectedLogEventDefault = JsonNode.Parse(
            """
            {"EventId":1001,"LogLevel":"Error","Category":"ConfigCat.Client.ConfigCatClient^","Message":"Failed to evaluate setting 'testFlag' (the key was not found in config JSON). Returning the `defaultValue` parameter that you specified in your application: ''. Available keys: [].","State":{"KEY":"testFlag","DEFAULT_PARAM_NAME":"defaultValue","DEFAULT_PARAM_VALUE":null,"AVAILABLE_KEYS":"","{OriginalFormat}":"Failed to evaluate setting '{KEY}' (the key was not found in config JSON). Returning the `{DEFAULT_PARAM_NAME}` parameter that you specified in your application: '{DEFAULT_PARAM_VALUE}'. Available keys: [{AVAILABLE_KEYS}]."}}
            """);

        var expectedLogEventNamed = JsonNode.Parse(
            """
            {"EventId":1001,"LogLevel":"Error","Category":"ConfigCat.Client.ConfigCatClient[my-client]","Message":"Failed to evaluate setting 'testFlag' (the key was not found in config JSON). Returning the `defaultValue` parameter that you specified in your application: ''. Available keys: [].","State":{"KEY":"testFlag","DEFAULT_PARAM_NAME":"defaultValue","DEFAULT_PARAM_VALUE":null,"AVAILABLE_KEYS":"","{OriginalFormat}":"Failed to evaluate setting '{KEY}' (the key was not found in config JSON). Returning the `{DEFAULT_PARAM_NAME}` parameter that you specified in your application: '{DEFAULT_PARAM_VALUE}'. Available keys: [{AVAILABLE_KEYS}]."}}
            """);

        Assert.IsTrue(logEvents.Any(evt => JsonNode.DeepEquals(evt, expectedLogEventDefault)));
        Assert.IsTrue(logEvents.Any(evt => JsonNode.DeepEquals(evt, expectedLogEventNamed)));
#endif
    }
}
