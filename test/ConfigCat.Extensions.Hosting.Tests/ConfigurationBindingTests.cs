using System;
using System.IO;
using System.Net;
using System.Text;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;
using ConfigCat.Extensions.Hosting.Configuration;
using ConfigCat.Extensions.Hosting.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Extensions.Hosting.Tests;

[TestClass]
public class ConfigurationBindingTests
{
    [DataTestMethod]
    [DataRow(nameof(PollingModes.AutoPoll))]
    [DataRow(nameof(PollingModes.LazyLoad))]
    [DataRow(nameof(PollingModes.ManualPoll))]
    [DataRow("<unspecified>")]
    [DataRow("<null>")]
    [DataRow("<mode-unspecified>")]
    [DataRow("<mode-null>")]
    public void ExtendedConfigCatClientOptions_BindingWrapper_Works(string pollingMode)
    {
        const string clientName = "my-client";

        var sdkKey = ClientConfigurationHelper.NewSdkKey();
        var pollInterval = TimeSpan.FromSeconds(1);
        var maxInitWaitTime = TimeSpan.FromSeconds(10);
        var cacheTimeToLive = TimeSpan.FromSeconds(5);
        var baseUri = new Uri("http://example.com");
        var proxyUri = new Uri("http://proxy.example.com:3128");
        var dataGovernance = DataGovernance.EuOnly;
        var httpTimeout = TimeSpan.FromMinutes(2);
        var offline = true;

        var pollingJson =
            $$"""
            {
              {{(
                  pollingMode == "<mode-unspecified>" ? ""
                  : pollingMode == "<mode-null>" ? "\"Mode\": null,"
                  : $"\"Mode\": \"{pollingMode}\","
              )}}
              "PollInterval": "{{pollInterval}}",
              "MaxInitWaitTime": "{{maxInitWaitTime}}",
              "CacheTimeToLive": "{{cacheTimeToLive}}",
            }
            """;

        var clientConfigurationJson =
            $$"""
            {
              "SdkKey": "{{sdkKey}}",
              {{(
                  pollingMode == "<unspecified>" ? ""
                  : pollingMode == "<null>" ? "\"Polling\": null,"
                  : $"\"Polling\": {pollingJson},"
              )}}
              "BaseUrl": "{{baseUri}}",
              "Proxy": "{{proxyUri}}",
              "DataGovernance": "{{dataGovernance}}",
              "HttpTimeout": "{{httpTimeout}}",
              "Offline": {{offline.ToString().ToLowerInvariant()}}
            }
            """;

        var configurationJson =
            $$"""
            {
              "ConfigCat": {
                "DefaultClient": {{clientConfigurationJson}},
                "NamedClients": {
                  "{{clientName}}": {{clientConfigurationJson}}
                }
              }
            }
            """;

        var configurationJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(configurationJson));

        var appBuilder = HostFactory.CreateMinimalHostBuilder();
        appBuilder.Configuration.AddJsonStream(configurationJsonStream);
        appBuilder.UseConfigCat();
        using var host = appBuilder.Build();

        void ResolveOptions(out ExtendedConfigCatClientOptions defaultClientOptions, out ExtendedConfigCatClientOptions namedClientOptions)
        {
            defaultClientOptions = host.Services.GetRequiredService<IOptions<ExtendedConfigCatClientOptions>>().Value;
            using (var scope = host.Services.CreateScope())
            {
                namedClientOptions = host.Services.GetRequiredService<IOptionsMonitor<ExtendedConfigCatClientOptions>>().Get(clientName);
            }
        }

        if (pollingMode == "<mode-null>")
        {
#if !NET9_0_OR_GREATER
            // NOTE: Before .NET 9, binding a null value to an enum property is not allowed.
            Assert.ThrowsException<InvalidOperationException>(() => ResolveOptions(out _, out _));
            return;
#endif
        }

        ResolveOptions(out var defaultClientOptions, out var namedClientOptions);

        AssertClientOptions(defaultClientOptions);
        AssertClientOptions(namedClientOptions);

        void AssertClientOptions(ExtendedConfigCatClientOptions clientOptions)
        {
            Assert.AreEqual(sdkKey, clientOptions.SdkKey);

            switch (pollingMode)
            {
                case nameof(PollingModes.AutoPoll):
                case "<mode-unspecified>":
                case "<mode-null>":
                    Assert.IsInstanceOfType<AutoPoll>(clientOptions.PollingMode);
                    var autoPoll = (AutoPoll)clientOptions.PollingMode;
                    Assert.AreEqual(pollInterval, autoPoll.PollInterval);
                    Assert.AreEqual(maxInitWaitTime, autoPoll.MaxInitWaitTime);
                    break;
                case nameof(PollingModes.LazyLoad):
                    Assert.IsInstanceOfType<LazyLoad>(clientOptions.PollingMode);
                    var lazyLoad = (LazyLoad)clientOptions.PollingMode;
                    Assert.AreEqual(cacheTimeToLive, lazyLoad.CacheTimeToLive);
                    break;
                case nameof(PollingModes.ManualPoll):
                    Assert.IsInstanceOfType<ManualPoll>(clientOptions.PollingMode);
                    break;
                case "<unspecified>":
                case "<null>":
                    Assert.IsNull(clientOptions.PollingMode);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pollingMode), pollingMode, null);
            }

            Assert.AreEqual(baseUri, clientOptions.BaseUrl);
            Assert.AreEqual(proxyUri, (clientOptions.Proxy as WebProxy)?.Address);
            Assert.IsFalse(clientOptions.Proxy!.IsBypassed(proxyUri));
            Assert.AreEqual(dataGovernance, clientOptions.DataGovernance);
            Assert.AreEqual(httpTimeout, clientOptions.HttpTimeout);
            Assert.AreEqual(offline, clientOptions.Offline);
        }
    }

    [DataTestMethod]
    [DataRow(nameof(ConfigCatInitMode.DoNotWaitForClientReady))]
    [DataRow(nameof(ConfigCatInitMode.WaitForClientReady))]
    [DataRow("<unspecified>")]
    [DataRow("<null>")]
    [DataRow("<mode-unspecified>")]
    [DataRow("<mode-null>")]
    public void ConfigCatInitializerOptions_BindingWrapper_Works(string initMode)
    {
        var throwOnFailure = true;

        var initJson =
            $$"""
            {
              {{(
                  initMode == "<mode-unspecified>" ? ""
                  : initMode == "<mode-null>" ? "\"Mode\": null,"
                  : $"\"Mode\": \"{initMode}\","
              )}}
              "ThrowOnFailure": {{throwOnFailure.ToString().ToLowerInvariant()}},
            }
            """;

        var configurationJson =
            $$"""
            {
              "ConfigCat": {
                {{(
                    initMode == "<unspecified>" ? ""
                    : initMode == "<null>" ? "\"Init\": null"
                    : $"\"Init\": {initJson}"
                )}}
              }
            }
            """;

        var configurationJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(configurationJson));

        var appBuilder = HostFactory.CreateMinimalHostBuilder();
        appBuilder.Configuration.AddJsonStream(configurationJsonStream);
        appBuilder.UseConfigCat();
        using var host = appBuilder.Build();

        void ResolveOptions(out ConfigCatInitializerOptions initializerOptions)
        {
            initializerOptions = host.Services.GetRequiredService<IOptions<ConfigCatInitializerOptions>>().Value;
        }

        if (initMode == "<mode-null>")
        {
#if !NET9_0_OR_GREATER
            // NOTE: Before .NET 9, binding a null value to an enum property is not allowed.
            Assert.ThrowsException<InvalidOperationException>(() => ResolveOptions(out _));
            return;
#endif
        }

        ResolveOptions(out var initializerOptions);

        switch (initMode)
        {
            case nameof(ConfigCatInitMode.DoNotWaitForClientReady):
            case "<mode-unspecified>":
            case "<mode-null>":
                Assert.IsInstanceOfType<ConfigCatInitMode.DoNotWaitForClientReady>(initializerOptions.InitMode.Value);
                break;
            case nameof(ConfigCatInitMode.WaitForClientReady):
                Assert.IsInstanceOfType<ConfigCatInitMode.WaitForClientReady>(initializerOptions.InitMode.Value);
                var waitForClientReadyMode = (ConfigCatInitMode.WaitForClientReady)initializerOptions.InitMode.Value;
                Assert.AreEqual(throwOnFailure, waitForClientReadyMode.ThrowOnFailure);
                break;
            case "<unspecified>":
            case "<null>":
                Assert.IsNull(initializerOptions.InitMode.Value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initMode), initMode, null);
        }
    }
}
