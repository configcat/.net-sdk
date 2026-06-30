using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Extensions.Hosting.Adapters;
using ConfigCat.Extensions.Hosting.Configuration;
using ConfigCat.Extensions.Hosting.Tests.Fakes;
using ConfigCat.Extensions.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Extensions.Hosting.Tests;

[TestClass]
[DoNotParallelize]
public class LoggingTests
{
    [TestMethod]
    public void LoggerAdapter_CorrectlyTranslatesLogLevelsAndForwardsParams()
    {
        var fakeLoggerProvider = new FakeMSLoggerProvider();

        var appBuilder = HostFactory.CreateMinimalHostBuilder();
        appBuilder.Logging
            .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
            .AddProvider(fakeLoggerProvider);

        appBuilder.UseConfigCat()
            .AddDefaultClient(options => options.SdkKey = ClientConfigurationHelper.NewSdkKey());
        using var host = appBuilder.Build();

        var defaultClientOptions = host.Services.GetRequiredService<IOptions<ExtendedConfigCatClientOptions>>().Value;

        Assert.IsInstanceOfType<ConfigCatToMSLoggerAdapter>(defaultClientOptions.Logger);

        var exception = new ApplicationException();
        var loggerWrapper = new LoggerWrapper(defaultClientOptions.Logger);
        loggerWrapper.Log(Client.LogLevel.Error, 1, exception, "Error message");
        loggerWrapper.Log(Client.LogLevel.Warning, 2, "Warning message");
        loggerWrapper.Log(Client.LogLevel.Info, 3, "Info message");
        loggerWrapper.Log(Client.LogLevel.Debug, 4, "Debug message");

        Assert.IsTrue(fakeLoggerProvider.Loggers.TryGetValue($"{typeof(ConfigCatClient).FullName}^", out var defaultClientLogger));

        Assert.IsTrue(defaultClientLogger.LogEvents.Any(evt =>
            evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Error
            && evt.eventId == 1
            && evt.message == "Error message"
            && ReferenceEquals(evt.exception, exception)));

        Assert.IsTrue(defaultClientLogger.LogEvents.Any(evt =>
            evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Warning
            && evt.eventId == 2
            && evt.message == "Warning message"
            && evt.exception is null));

        Assert.IsTrue(defaultClientLogger.LogEvents.Any(evt =>
            evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Information
            && evt.eventId == 3
            && evt.message == "Info message"
            && evt.exception is null));

        Assert.IsTrue(defaultClientLogger.LogEvents.Any(evt =>
            evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Debug
            && evt.eventId == 4
            && evt.message == "Debug message"
            && evt.exception is null));
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public async Task LoggerAdapter_StructuredLoggingWorks(HostKind hostKind)
    {
#if NET10_0_OR_GREATER
        // NOTE: Console redirection doesn't seem to work for console logger before .NET 10.
        var consoleOutput = new StringBuilder();
        var originalConsoleOut = Console.Out;
        Console.SetOut(new StringWriter(consoleOutput));
#endif
        try
        {
            const string clientName = "my-client";

            IHost host;

            var faultyConfigCache = new FaultyConfigCache();

            Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
            {
                Action<ExtendedConfigCatClientOptions> configureClient = options =>
                {
                    options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Offline = true;
                    options.ConfigCache = faultyConfigCache;
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

                faultyConfigCache.ShouldFail = true;
                await defaultClient.ForceRefreshAsync();
            }

#if NET10_0_OR_GREATER
            var reader = new StringReader(consoleOutput.ToString());
            var logEvents = new List<JsonNode?>();
            while (reader.ReadLine() is { } line)
            {
                var jsonObject = JsonNode.Parse(line)!.AsObject();
                jsonObject.Remove("Exception");
                logEvents.Add(jsonObject);
            }

            var expectedLogEvent1000_Default = JsonNode.Parse(
                """
                {"EventId":1000,"LogLevel":"Error","Category":"ConfigCat.Client.ConfigCatClient^","Message":"Config JSON is not present when evaluating setting 'testFlag'. Returning the `defaultValue` parameter that you specified in your application: ''.","State":{"KEY":"testFlag","DEFAULT_PARAM_NAME":"defaultValue","DEFAULT_PARAM_VALUE":null,"{OriginalFormat}":"Config JSON is not present when evaluating setting '{KEY}'. Returning the `{DEFAULT_PARAM_NAME}` parameter that you specified in your application: '{DEFAULT_PARAM_VALUE}'."}}
                """);

            var expectedLogEvent1000_Named = JsonNode.Parse(
                """
                {"EventId":1000,"LogLevel":"Error","Category":"ConfigCat.Client.ConfigCatClient[my-client]","Message":"Config JSON is not present when evaluating setting 'testFlag'. Returning the `defaultValue` parameter that you specified in your application: ''.","State":{"KEY":"testFlag","DEFAULT_PARAM_NAME":"defaultValue","DEFAULT_PARAM_VALUE":null,"{OriginalFormat}":"Config JSON is not present when evaluating setting '{KEY}'. Returning the `{DEFAULT_PARAM_NAME}` parameter that you specified in your application: '{DEFAULT_PARAM_VALUE}'."}}
                """);

            var expectedLogEvent2200 = JsonNode.Parse(
                """
                {"EventId":2200,"LogLevel":"Error","Category":"ConfigCat.Client.ConfigCatClient^","Message":"Error occurred while reading the cache.","State":{"{OriginalFormat}":"Error occurred while reading the cache."}}
                """);

            Assert.IsTrue(logEvents.Any(evt => JsonNode.DeepEquals(evt, expectedLogEvent1000_Default)));
            Assert.IsTrue(logEvents.Any(evt => JsonNode.DeepEquals(evt, expectedLogEvent1000_Named)));
            Assert.IsTrue(logEvents.Any(evt => JsonNode.DeepEquals(evt, expectedLogEvent2200)));
#endif
        }
        finally
        {
#if NET10_0_OR_GREATER
            Console.SetOut(originalConsoleOut);
#endif
        }
    }

    private sealed class FaultyConfigCache : IConfigCatCache
    {
        public bool ShouldFail { get; set; }

        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => !ShouldFail ? default : throw new InvalidOperationException();

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
            => !ShouldFail ? default : throw new InvalidOperationException();
    }
}
