using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Tests.Fakes;
using ConfigCat.Extensions.Hosting.Tests.Fakes;
using ConfigCat.Extensions.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Extensions.Hosting.Tests;

[TestClass]
public class ConfigCatInitializerTests
{
    [TestMethod]
    public async Task InitializeAsync_DoNotWaitForClientReady_ReturnsBeforeClientIsReady()
    {
        // Arrange

        const int maxInitWaitTimeSeconds = 5;

        var services = new ServiceCollection();
        services.AddConfigCat(builder =>
        {
            builder
                .AddDefaultClient(options =>
                {
                    options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                    options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(maxInitWaitTimeSeconds));
                    options.ConfigFetcher = CreateFailingFetcher();
                })
                .UseInitMode(new ConfigCatInitMode.DoNotWaitForClientReady());
        });

        using var host = new FakeHost(services.BuildServiceProvider());
        var initializer = host.Services.GetRequiredService<IConfigCatInitializer>();

        // Act

        var sw = Stopwatch.StartNew();
        await initializer.InitializeAsync();
        sw.Stop();

        // Assert

        var defaultClient = host.Services.GetRequiredService<IConfigCatClient>();
        Assert.AreEqual(ClientCacheState.NoFlagData, defaultClient.Snapshot().CacheState);

        Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(maxInitWaitTimeSeconds / 2.0),
            $"Expected {nameof(ConfigCatInitializer.InitializeAsync)} to return before {nameof(AutoPoll.MaxInitWaitTime)}, but it took {sw.Elapsed}.");
    }

    [TestMethod]
    public async Task InitializeAsync_WaitForClientReady_DoNotThrowOnFailure_CompletesWithoutException()
    {
        // Arrange

        var services = new ServiceCollection();
        services.AddConfigCat(builder =>
        {
            builder
                .AddDefaultClient(options =>
                {
                    options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                    options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(1));
                    options.ConfigFetcher = CreateFailingFetcher();
                })
                .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: false));
        });

        using var host = new FakeHost(services.BuildServiceProvider());
        var initializer = host.Services.GetRequiredService<IConfigCatInitializer>();

        // Act

        await initializer.InitializeAsync(CancellationToken.None);

        // Assert

        var defaultClient = host.Services.GetRequiredService<IConfigCatClient>();
        Assert.AreEqual(ClientCacheState.NoFlagData, defaultClient.Snapshot().CacheState);
    }

    [TestMethod]
    public async Task InitializeAsync_WaitForClientReady_ThrowOnFailure_ThrowsTimeoutException()
    {
        // Arrange

        var services = new ServiceCollection();
        services.AddConfigCat(builder =>
        {
            builder
                .AddDefaultClient(options =>
                {
                    options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                    options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(1));
                    options.ConfigFetcher = CreateFailingFetcher();
                })
                .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: true));
        });

        using var host = new FakeHost(services.BuildServiceProvider());
        var initializer = host.Services.GetRequiredService<IConfigCatInitializer>();

        // Act & Assert

        var ex = await Assert.ThrowsExceptionAsync<TimeoutException>(
            () => initializer.InitializeAsync(CancellationToken.None));

        StringAssert.StartsWith(ex.Message, $"One or more {nameof(IConfigCatClient)} services failed to initialize within {nameof(AutoPoll.MaxInitWaitTime)}:");
    }

    [TestMethod]
    public async Task InitializeAsync_WaitForClientReady_ThrowOnFailure_SuccessfulFetch_DoesNotThrow()
    {
        // Arrange

        var services = new ServiceCollection();
        services.AddConfigCat(builder =>
        {
            builder
                .AddDefaultClient(options =>
                {
                    options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                    options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(1));
                    options.ConfigFetcher = new FakeConfigFetcher(_ => Task.FromResult(
                        new FetchResponse(HttpStatusCode.OK,
                        "OK",
                        Array.Empty<KeyValuePair<string, string>>(),
                        "{}")));
                })
                .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: true));
        });

        using var host = new FakeHost(services.BuildServiceProvider());
        var initializer = host.Services.GetRequiredService<IConfigCatInitializer>();

        // Act

        await initializer.InitializeAsync(CancellationToken.None);

        // Assert

        var defaultClient = host.Services.GetRequiredService<IConfigCatClient>();
        Assert.AreEqual(ClientCacheState.HasUpToDateFlagData, defaultClient.Snapshot().CacheState);
    }

    private static FakeConfigFetcher CreateFailingFetcher()
    {
        return new FakeConfigFetcher(_ => Task.FromResult(
            new FetchResponse(HttpStatusCode.BadGateway,
            "Bad Gateway",
            Array.Empty<KeyValuePair<string, string>>())));
    }
}
