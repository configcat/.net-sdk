using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Tests;
using ConfigCat.Extensions.Hosting.Configuration;
using ConfigCat.Extensions.Hosting.Tests.Fakes;
using ConfigCat.Extensions.Hosting.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Extensions.Hosting.Tests;

[TestClass]
public class ServiceRegistrationTests
{
    #region Client registration by code

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public void AddDefaultClient_RegistersDefaultClientAsSingletonAndSnapshotAsScoped(HostKind hostKind)
    {
        IHost host;

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            builder.AddDefaultClient(options =>
            {
                options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                options.FlagOverrides = FlagOverrides.LocalDictionary(new Dictionary<string, object>(), OverrideBehaviour.LocalOnly);
            });
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(setupConfigCatBuilder);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var client = host.Services.GetService<IConfigCatClient>();
        Assert.IsNotNull(client);

        var clients = host.Services.GetServices<IConfigCatClient>();
        Assert.AreEqual(1, clients.Count());
        Assert.AreSame(client, clients.First());

        using (var scope = host.Services.CreateScope())
        {
            Assert.AreSame(client, scope.ServiceProvider.GetService<IConfigCatClient>());

            var snapshot = scope.ServiceProvider.GetService<IConfigCatClientSnapshot>();
            Assert.IsNotNull(snapshot);

            var snapshots = scope.ServiceProvider.GetServices<IConfigCatClientSnapshot>();
            Assert.AreEqual(1, snapshots.Count());
            Assert.AreSame(snapshot, snapshots.First());

            using (var scope2 = host.Services.CreateScope())
            {
                Assert.AreNotSame(snapshot, scope2.ServiceProvider.GetService<IConfigCatClientSnapshot>());
            }

#if NET10_0_OR_GREATER
            Assert.AreEqual(0, scope.ServiceProvider.GetKeyedServices<IConfigCatClientSnapshot>(KeyedService.AnyKey).Count());
#endif
        }

#if NET10_0_OR_GREATER
        Assert.AreEqual(0, host.Services.GetKeyedServices<IConfigCatClient>(KeyedService.AnyKey).Count());
#endif
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public void AddNamedClient_RegistersNamedClientAsSingletonAndSnapshotAsScoped(HostKind hostKind)
    {
        const string clientName = "my-client";

        IHost host;

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            builder.AddNamedClient(clientName, options =>
            {
                options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                options.FlagOverrides = FlagOverrides.LocalDictionary(new Dictionary<string, object>(), OverrideBehaviour.LocalOnly);
            });
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(setupConfigCatBuilder);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var client = host.Services.GetKeyedService<IConfigCatClient>(clientName);
        Assert.IsNotNull(client);

        var clients = host.Services.GetKeyedServices<IConfigCatClient>(clientName);
        Assert.AreEqual(1, clients.Count());
        Assert.AreSame(client, clients.First());

#if NET10_0_OR_GREATER
        clients = host.Services.GetKeyedServices<IConfigCatClient>(KeyedService.AnyKey);
        Assert.AreEqual(1, clients.Count());
        Assert.AreSame(client, clients.First());
#endif

        using (var scope = host.Services.CreateScope())
        {
            Assert.AreSame(client, host.Services.GetKeyedService<IConfigCatClient>(clientName));

            var snapshot = scope.ServiceProvider.GetKeyedService<IConfigCatClientSnapshot>(clientName);
            Assert.IsNotNull(snapshot);

            var snapshots = scope.ServiceProvider.GetKeyedServices<IConfigCatClientSnapshot>(clientName);
            Assert.AreEqual(1, snapshots.Count());
            Assert.AreSame(snapshot, snapshots.First());

#if NET10_0_OR_GREATER
            snapshots = scope.ServiceProvider.GetKeyedServices<IConfigCatClientSnapshot>(KeyedService.AnyKey);
            Assert.AreEqual(1, snapshots.Count());
            Assert.AreSame(snapshot, snapshots.First());
#endif

            using (var scope2 = host.Services.CreateScope())
            {
                Assert.AreNotSame(snapshot, scope2.ServiceProvider.GetKeyedService<IConfigCatClientSnapshot>(clientName));
            }

            Assert.IsNull(scope.ServiceProvider.GetService<IConfigCatClientSnapshot>());
        }

        Assert.IsNull(host.Services.GetService<IConfigCatClient>());
    }

    #endregion

    #region Client registration by configuration

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public void DefaultClientConfigurationSection_RegistersDefaultClientAsSingletonAndSnapshotAsScoped(HostKind hostKind)
    {
        IHost host;

        Action<IConfigurationBuilder> setupConfiguration = builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConfigCat:DefaultClient:SdkKey"] = ClientConfigurationHelper.NewSdkKey(),
                ["ConfigCat:DefaultClient:Polling:Mode"] = nameof(PollingModes.ManualPoll),
                ["ConfigCat:DefaultClient:Offline"] = "true",
            });
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfiguration(appBuilder.Configuration);
                appBuilder.UseConfigCat();
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureAppConfiguration(setupConfiguration);
                hostBuilder.ConfigureConfigCat();
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var configurationBuilder = new ConfigurationBuilder();
                setupConfiguration(configurationBuilder);
                var configuration = configurationBuilder.Build();
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(configuration);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var client = host.Services.GetService<IConfigCatClient>();
        Assert.IsNotNull(client);

        var clients = host.Services.GetServices<IConfigCatClient>();
        Assert.AreEqual(1, clients.Count());
        Assert.AreSame(client, clients.First());

        using (var scope = host.Services.CreateScope())
        {
            Assert.AreSame(client, scope.ServiceProvider.GetService<IConfigCatClient>());

            var snapshot = scope.ServiceProvider.GetService<IConfigCatClientSnapshot>();
            Assert.IsNotNull(snapshot);

            var snapshots = scope.ServiceProvider.GetServices<IConfigCatClientSnapshot>();
            Assert.AreEqual(1, snapshots.Count());
            Assert.AreSame(snapshot, snapshots.First());

            using (var scope2 = host.Services.CreateScope())
            {
                Assert.AreNotSame(snapshot, scope2.ServiceProvider.GetService<IConfigCatClientSnapshot>());
            }

#if NET10_0_OR_GREATER
            Assert.AreEqual(0, scope.ServiceProvider.GetKeyedServices<IConfigCatClientSnapshot>(KeyedService.AnyKey).Count());
#endif
        }

#if NET10_0_OR_GREATER
        Assert.AreEqual(0, host.Services.GetKeyedServices<IConfigCatClient>(KeyedService.AnyKey).Count());
#endif
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public void NamedClientConfigurationSection_RegistersNamedClientAsSingletonAndSnapshotAsScoped(HostKind hostKind)
    {
        const string clientName = "my-client";

        IHost host;

        Action<IConfigurationBuilder> setupConfiguration = builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConfigCat:NamedClients:{clientName}:SdkKey"] = ClientConfigurationHelper.NewSdkKey(),
                [$"ConfigCat:NamedClients:{clientName}:Polling:Mode"] = nameof(PollingModes.ManualPoll),
                [$"ConfigCat:NamedClients:{clientName}:Offline"] = "true",
            });
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfiguration(appBuilder.Configuration);
                appBuilder.UseConfigCat();
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureAppConfiguration(setupConfiguration);
                hostBuilder.ConfigureConfigCat();
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var configurationBuilder = new ConfigurationBuilder();
                setupConfiguration(configurationBuilder);
                var configuration = configurationBuilder.Build();
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(configuration);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var client = host.Services.GetKeyedService<IConfigCatClient>(clientName);
        Assert.IsNotNull(client);

        var clients = host.Services.GetKeyedServices<IConfigCatClient>(clientName);
        Assert.AreEqual(1, clients.Count());
        Assert.AreSame(client, clients.First());

#if NET10_0_OR_GREATER
        clients = host.Services.GetKeyedServices<IConfigCatClient>(KeyedService.AnyKey);
        Assert.AreEqual(1, clients.Count());
        Assert.AreSame(client, clients.First());
#endif

        using (var scope = host.Services.CreateScope())
        {
            Assert.AreSame(client, host.Services.GetKeyedService<IConfigCatClient>(clientName));

            var snapshot = scope.ServiceProvider.GetKeyedService<IConfigCatClientSnapshot>(clientName);
            Assert.IsNotNull(snapshot);

            var snapshots = scope.ServiceProvider.GetKeyedServices<IConfigCatClientSnapshot>(clientName);
            Assert.AreEqual(1, snapshots.Count());
            Assert.AreSame(snapshot, snapshots.First());

#if NET10_0_OR_GREATER
            snapshots = scope.ServiceProvider.GetKeyedServices<IConfigCatClientSnapshot>(KeyedService.AnyKey);
            Assert.AreEqual(1, snapshots.Count());
            Assert.AreSame(snapshot, snapshots.First());
#endif

            using (var scope2 = host.Services.CreateScope())
            {
                Assert.AreNotSame(snapshot, scope2.ServiceProvider.GetKeyedService<IConfigCatClientSnapshot>(clientName));
            }

            Assert.IsNull(scope.ServiceProvider.GetService<IConfigCatClientSnapshot>());
        }

        Assert.IsNull(host.Services.GetService<IConfigCatClient>());
    }

    #endregion

    #region Multiple client registrations

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public void SetupClientsByCodeTakesPrecendenceOverConfiguration_ButDoesNotResultInMultipleRegistrations(HostKind hostKind)
    {
        const string client1Name = "my-client", client2Name = "my-client-2";

        IHost host;

        Action<IConfigurationBuilder> setupConfiguration = builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConfigCat:DefaultClient:SdkKey"] = "invalid-sdk-key",
                [$"ConfigCat:DefaultClient:Offline"] = "false",
                [$"ConfigCat:NamedClients:{client1Name}:SdkKey"] = "invalid-sdk-key",
                [$"ConfigCat:NamedClients:{client1Name}:Offline"] = "false",
                [$"ConfigCat:NamedClients:{client2Name}:SdkKey"] = ClientConfigurationHelper.NewSdkKey(),
                [$"ConfigCat:NamedClients:{client2Name}:PollingMode"] = nameof(PollingModes.ManualPoll),
                [$"ConfigCat:NamedClients:{client2Name}:Offline"] = "true",
            });
        };

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            Action<ExtendedConfigCatClientOptions> configureClient = options =>
            {
                options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                options.FlagOverrides = FlagOverrides.LocalDictionary(new Dictionary<string, object> { ["testFlag"] = true }, OverrideBehaviour.LocalOnly);
            };

            builder
                .AddDefaultClient(configureClient)
                .AddNamedClient(client1Name, configureClient);
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfiguration(appBuilder.Configuration);
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureAppConfiguration(setupConfiguration);
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var configurationBuilder = new ConfigurationBuilder();
                setupConfiguration(configurationBuilder);
                var configuration = configurationBuilder.Build();
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(configuration, setupConfigCatBuilder);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var defaultClient = host.Services.GetService<IConfigCatClient>();
        Assert.IsNotNull(defaultClient);
        Assert.IsTrue(defaultClient.IsOffline); // clients using local-only overrides are offline by default
        Assert.IsTrue(defaultClient.Snapshot().GetValue("testFlag", (bool?)null));

        var namedClient1 = host.Services.GetKeyedService<IConfigCatClient>(client1Name);
        Assert.IsNotNull(namedClient1);
        Assert.IsTrue(namedClient1.IsOffline); // clients using local-only overrides are offline by default
        Assert.IsTrue(namedClient1.Snapshot().GetValue("testFlag", (bool?)null));

        var namedClient2 = host.Services.GetKeyedService<IConfigCatClient>(client2Name);
        Assert.IsNotNull(namedClient2);
        Assert.IsTrue(namedClient2.IsOffline);
        Assert.IsNull(namedClient2.Snapshot().GetValue("testFlag", (bool?)null));

        Assert.AreEqual(1, host.Services.GetServices<IConfigCatClient>().Count());

#if NET10_0_OR_GREATER
        Assert.AreEqual(2, host.Services.GetKeyedServices<IConfigCatClient>(KeyedService.AnyKey).Count());
#endif
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public void SetupClientsByCodeTakesPrecendenceOverEarlier_ButDoesNotResultInMultipleRegistrations(HostKind hostKind)
    {
        const string client1Name = "my-client", client2Name = "my-client-2";

        IHost host;

        Action<ConfigCatBuilder> setupConfigCatBuilder1 = builder =>
        {
            builder
                .AddDefaultClient(options => options.SdkKey = "invalid-sdk-key")
                .AddNamedClient(client1Name, options => options.SdkKey = "invalid-sdk-key")
                .AddNamedClient(client2Name, options =>
                {
                    options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Offline = true;
                });
        };

        Action<ConfigCatBuilder> setupConfigCatBuilder2 = builder =>
        {
            Action<ExtendedConfigCatClientOptions> configureClient = options =>
            {
                options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                options.FlagOverrides = FlagOverrides.LocalDictionary(new Dictionary<string, object> { ["testFlag"] = true }, OverrideBehaviour.LocalOnly);
            };

            builder
                .AddDefaultClient(configureClient)
                .AddNamedClient(client1Name, configureClient);
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfigCatBuilder1(appBuilder.UseConfigCat());
                setupConfigCatBuilder2(appBuilder.UseConfigCat());
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder1);
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder2);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(setupConfigCatBuilder1);
                serviceCollection.AddConfigCat(setupConfigCatBuilder2);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var defaultClient = host.Services.GetService<IConfigCatClient>();
        Assert.IsNotNull(defaultClient);
        Assert.IsTrue(defaultClient.IsOffline); // clients using local-only overrides are offline by default
        Assert.IsTrue(defaultClient.Snapshot().GetValue("testFlag", (bool?)null));

        var namedClient1 = host.Services.GetKeyedService<IConfigCatClient>(client1Name);
        Assert.IsNotNull(namedClient1);
        Assert.IsTrue(namedClient1.IsOffline); // clients using local-only overrides are offline by default
        Assert.IsTrue(namedClient1.Snapshot().GetValue("testFlag", (bool?)null));

        var namedClient2 = host.Services.GetKeyedService<IConfigCatClient>(client2Name);
        Assert.IsNotNull(namedClient2);
        Assert.IsTrue(namedClient2.IsOffline);
        Assert.IsNull(namedClient2.Snapshot().GetValue("testFlag", (bool?)null));

        Assert.AreEqual(1, host.Services.GetServices<IConfigCatClient>().Count());

#if NET10_0_OR_GREATER
        Assert.AreEqual(2, host.Services.GetKeyedServices<IConfigCatClient>(KeyedService.AnyKey).Count());
#endif
    }

    #endregion

    #region Initializer service registration

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public void Initializer_GetsRegistered(HostKind hostKind)
    {
        IHost host;

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                appBuilder.UseConfigCat();
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat();
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var configuration = new ConfigurationBuilder().Build();
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(configuration);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        Assert.IsNotNull(host.Services.GetService<IConfigCatInitializer>());
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    public async Task Initializer_RunsAutomatically(HostKind hostKind)
    {
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
                .AddNamedClient(clientName, configureClient)
                .UseInitMode(new ConfigCatInitMode.WaitForClientReady());
        };

        var typedFakeLogger = FakeMSLogger.Create<ConfigCatInitializer>(out var fakeLogger);
        Action<IServiceCollection> setupServices = services =>
        {
            services.AddSingleton<ILogger<ConfigCatInitializer>>(typedFakeLogger);
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                setupServices(appBuilder.Services);
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                hostBuilder.ConfigureServices(setupServices);
                host = hostBuilder.Build();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        Assert.IsNotNull(host.Services.GetService<IConfigCatInitializer>());

        await host.StartAsync();

        Assert.AreEqual(1, fakeLogger.LogEvents.Count(item =>
            item.logLevel == Microsoft.Extensions.Logging.LogLevel.Information
            && item.message == $"All registered {nameof(ConfigCatClient)} instances are initialized and ready to evaluate feature flags."));

        await host.StopAsync();
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public async Task Initializer_CanBeSetupByCode(HostKind hostKind)
    {
        IHost host;

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            builder
                .AddDefaultClient(options => options.SdkKey = ClientConfigurationHelper.NewSdkKey(ensureNonExistent: true))
                .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: true));
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(setupConfigCatBuilder);
                host = new FakeHost(serviceCollection.BuildServiceProvider(),
                    startAsync: (sp, ct) => sp.GetRequiredService<IConfigCatInitializer>().InitializeAsync(ct));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        Assert.IsNotNull(host.Services.GetService<IConfigCatInitializer>());

        var ex = await Assert.ThrowsExceptionAsync<TimeoutException>(async () => await host.StartAsync());

        Assert.AreEqual($"One or more {nameof(ConfigCatClient)} instances failed to initialize within {nameof(AutoPoll.MaxInitWaitTime)}: (default).", ex.Message);

        await host.StopAsync();
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public async Task Initializer_CanBeSetupByConfiguration(HostKind hostKind)
    {
        IHost host;

        Action<IConfigurationBuilder> setupConfiguration = builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConfigCat:Init:Mode"] = nameof(ConfigCatInitMode.WaitForClientReady),
                ["ConfigCat:Init:ThrowOnFailure"] = "true",
            });
        };

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            builder.AddDefaultClient(options => options.SdkKey = ClientConfigurationHelper.NewSdkKey(ensureNonExistent: true));
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfiguration(appBuilder.Configuration);
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureAppConfiguration(setupConfiguration);
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var configurationBuilder = new ConfigurationBuilder();
                setupConfiguration(configurationBuilder);
                var configuration = configurationBuilder.Build();
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(configuration, setupConfigCatBuilder);
                host = new FakeHost(serviceCollection.BuildServiceProvider(),
                    startAsync: (sp, ct) => sp.GetRequiredService<IConfigCatInitializer>().InitializeAsync(ct));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        Assert.IsNotNull(host.Services.GetService<IConfigCatInitializer>());

        var ex = await Assert.ThrowsExceptionAsync<TimeoutException>(async () => await host.StartAsync());

        Assert.AreEqual($"One or more {nameof(ConfigCatClient)} instances failed to initialize within {nameof(AutoPoll.MaxInitWaitTime)}: (default).", ex.Message);

        await host.StopAsync();
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public async Task Initializer_SetupByCodeTakesPrecendenceOverConfiguration(HostKind hostKind)
    {
        const string clientName = "my-client";

        IHost host;

        Action<IConfigurationBuilder> setupConfiguration = builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConfigCat:Init:Mode"] = nameof(ConfigCatInitMode.WaitForClientReady),
                ["ConfigCat:Init:ThrowOnFailure"] = "true",
                [$"ConfigCat:NamedClients:{clientName}:SdkKey"] = ClientConfigurationHelper.NewSdkKey(ensureNonExistent: true),
                [$"ConfigCat:NamedClients:{clientName}:Polling:Mode"] = nameof(PollingModes.AutoPoll),
            });
        };

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            builder
                .AddDefaultClient(options =>
                {
                    options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                    options.FlagOverrides = FlagOverrides.LocalDictionary(new Dictionary<string, object>(), OverrideBehaviour.LocalOnly);
                })
                .UseInitMode(new ConfigCatInitMode.WaitForClientReady());
        };

        var typedFakeLogger = FakeMSLogger.Create<ConfigCatInitializer>(out var fakeLogger);
        Action<IServiceCollection> setupServices = services =>
        {
            services.AddSingleton<ILogger<ConfigCatInitializer>>(typedFakeLogger);
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfiguration(appBuilder.Configuration);
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                setupServices(appBuilder.Services);
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureAppConfiguration(setupConfiguration);
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                hostBuilder.ConfigureServices(setupServices);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var configurationBuilder = new ConfigurationBuilder();
                setupConfiguration(configurationBuilder);
                var configuration = configurationBuilder.Build();
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(configuration, setupConfigCatBuilder);
                setupServices(serviceCollection);
                host = new FakeHost(serviceCollection.BuildServiceProvider(),
                    startAsync: (sp, ct) => sp.GetRequiredService<IConfigCatInitializer>().InitializeAsync(ct));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        Assert.IsNotNull(host.Services.GetService<IConfigCatInitializer>());

        await host.StartAsync();

        Assert.AreEqual(1, fakeLogger.LogEvents.Count(item =>
            item.logLevel == Microsoft.Extensions.Logging.LogLevel.Information
            && item.message.ToString() == $"Waiting for 2 {nameof(ConfigCatClient)} instance(s) to initalize..."));

        Assert.AreEqual(1, fakeLogger.LogEvents.Count(item =>
            item.logLevel == Microsoft.Extensions.Logging.LogLevel.Warning
            && item.message.ToString() == $"One or more {nameof(ConfigCatClient)} instances failed to initialize within {nameof(AutoPoll.MaxInitWaitTime)}: '{clientName}'. They may still be able to initialize later."));

        await host.StopAsync();
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public async Task Initializer_SetupByCodeTakesPrecendenceOverEarlier_ButDoesNotResultInMultipleRegistrations(HostKind hostKind)
    {
        const string clientName = "my-client";

        IHost host;

        Action<ExtendedConfigCatClientOptions> configureClient = options =>
        {
            options.SdkKey = ClientConfigurationHelper.NewSdkKey();
            options.FlagOverrides = FlagOverrides.LocalDictionary(new Dictionary<string, object>(), OverrideBehaviour.LocalOnly);
        };

        Action<ConfigCatBuilder> setupConfigCatBuilder1 = builder =>
        {
            builder
                .AddNamedClient(clientName, configureClient)
                .UseInitMode(new ConfigCatInitMode.WaitForClientReady(throwOnFailure: true));
        };

        Action<ConfigCatBuilder> setupConfigCatBuilder2 = builder =>
        {
            builder
                .AddDefaultClient(configureClient)
                .UseInitMode(new ConfigCatInitMode.DoNotWaitForClientReady());
        };

        var typedFakeLogger = FakeMSLogger.Create<ConfigCatInitializer>(out var fakeLogger);
        Action<IServiceCollection> setupServices = services =>
        {
            services.AddSingleton<ILogger<ConfigCatInitializer>>(typedFakeLogger);
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfigCatBuilder1(appBuilder.UseConfigCat());
                setupConfigCatBuilder2(appBuilder.UseConfigCat());
                setupServices(appBuilder.Services);
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder1);
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder2);
                hostBuilder.ConfigureServices(setupServices);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(setupConfigCatBuilder1);
                serviceCollection.AddConfigCat(setupConfigCatBuilder2);
                setupServices(serviceCollection);
                host = new FakeHost(serviceCollection.BuildServiceProvider(),
                    startAsync: (sp, ct) => sp.GetRequiredService<IConfigCatInitializer>().InitializeAsync(ct));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        Assert.IsNotNull(host.Services.GetService<IConfigCatInitializer>());

        await host.StartAsync();

        Assert.AreEqual(1, fakeLogger.LogEvents.Count(item =>
            item.logLevel == Microsoft.Extensions.Logging.LogLevel.Information
            && item.message == $"All registered {nameof(ConfigCatClient)} instances are created but may still be initializing."));

        await host.StopAsync();
    }

    #endregion

    #region Logger adapter configuration

    [DataTestMethod]
    [DataRow(HostKind.Minimal)]
    [DataRow(HostKind.Legacy)]
    [DataRow(HostKind.PlainDI)]
    public async Task LoggerAdapter_GetsConfigured(HostKind hostKind)
    {
        const string client1Name = "my-client", client2Name = "my-client-2";

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
                .AddNamedClient(client1Name, configureClient)
                .AddNamedClient(client2Name, configureClient);
        };

        var fakeLoggerProvider = new FakeMSLoggerProvider();
        Action<ILoggingBuilder> setupLogging = builder =>
        {
            builder.AddProvider(fakeLoggerProvider);
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

        using var _ = host;

        var defaultClient = host.Services.GetRequiredKeyedService<IConfigCatClient>(null);
        var namedClient1 = host.Services.GetRequiredKeyedService<IConfigCatClient>(client1Name);
        var namedClient2 = host.Services.GetRequiredKeyedService<IConfigCatClient>(client2Name);

        Assert.IsNull(await defaultClient.GetValueAsync("testFlag", (bool?)null));
        Assert.IsNull(await namedClient1.GetValueAsync("testFlag", (bool?)null));
        Assert.IsNull(await namedClient2.GetValueAsync("testFlag", (bool?)null));

        Assert.IsTrue(fakeLoggerProvider.Loggers.TryGetValue($"{typeof(ConfigCatClient).FullName}^", out var defaultClientLogger));
        Assert.IsTrue(fakeLoggerProvider.Loggers.TryGetValue($"{typeof(ConfigCatClient).FullName}[{client1Name}]", out var namedClient1Logger));
        Assert.IsTrue(fakeLoggerProvider.Loggers.TryGetValue($"{typeof(ConfigCatClient).FullName}[{client2Name}]", out var namedClient2Logger));

        Assert.IsTrue(defaultClientLogger.LogEvents.Any(evt => evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Error && evt.eventId == 1001));
        Assert.IsTrue(namedClient1Logger.LogEvents.Any(evt => evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Error && evt.eventId == 1001));
        Assert.IsTrue(namedClient2Logger.LogEvents.Any(evt => evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Error && evt.eventId == 1001));
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal, false)]
    [DataRow(HostKind.Minimal, true)]
    [DataRow(HostKind.Legacy, false)]
    [DataRow(HostKind.Legacy, true)]
    [DataRow(HostKind.PlainDI, false)]
    [DataRow(HostKind.PlainDI, true)]
    public async Task LoggerAdapter_ManualConfigurationTakesPrecedence(HostKind hostKind, bool registerClientFirst)
    {
        const string clientName = "my-client";

        IHost host;

        var fakeConfigCatLoggerDefault = new FakeConfigCatLogger();
        var fakeConfigCatLoggerNamed = new FakeConfigCatLogger();

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

        Action<IServiceCollection> setupServices = services =>
        {
            services.Configure<ExtendedConfigCatClientOptions>(options => options.Logger = fakeConfigCatLoggerDefault);
            services.Configure<ExtendedConfigCatClientOptions>(clientName, options => options.Logger = fakeConfigCatLoggerNamed);
        };

        Action<IServiceCollection> setupServicesBefore = !registerClientFirst ? setupServices : _ => { };
        Action<IServiceCollection> setupServicesAfter = registerClientFirst ? setupServices : _ => { };

        var fakeLoggerProvider = new FakeMSLoggerProvider();
        Action<ILoggingBuilder> setupLogging = builder =>
        {
            builder.AddProvider(fakeLoggerProvider);
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupLogging(appBuilder.Logging);
                setupServicesBefore(appBuilder.Services);
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                setupServicesAfter(appBuilder.Services);
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureLogging(setupLogging);
                hostBuilder.ConfigureServices(setupServicesBefore);
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                hostBuilder.ConfigureServices(setupServicesAfter);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddLogging(setupLogging);
                setupServicesBefore(serviceCollection);
                serviceCollection.AddConfigCat(setupConfigCatBuilder);
                setupServicesAfter(serviceCollection);
                host = new FakeHost(serviceCollection.BuildServiceProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var defaultClient = host.Services.GetRequiredKeyedService<IConfigCatClient>(null);
        var namedClient1 = host.Services.GetRequiredKeyedService<IConfigCatClient>(clientName);

        Assert.IsNull(await defaultClient.GetValueAsync("testFlag", (bool?)null));
        Assert.IsNull(await namedClient1.GetValueAsync("testFlag", (bool?)null));

        Assert.IsTrue(fakeLoggerProvider.Loggers.TryGetValue($"{typeof(ConfigCatClient).FullName}^", out var defaultClientLogger));
        Assert.IsTrue(fakeLoggerProvider.Loggers.TryGetValue($"{typeof(ConfigCatClient).FullName}[{clientName}]", out var namedClientLogger));

        Assert.IsFalse(defaultClientLogger.LogEvents.Any(evt => evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Error && evt.eventId == 1001));
        Assert.IsFalse(namedClientLogger.LogEvents.Any(evt => evt.logLevel == Microsoft.Extensions.Logging.LogLevel.Error && evt.eventId == 1001));

        Assert.IsTrue(fakeConfigCatLoggerDefault.LogEvents.Any(evt => evt.logLevel == Client.LogLevel.Error && evt.eventId == 1001));
        Assert.IsTrue(fakeConfigCatLoggerNamed.LogEvents.Any(evt => evt.logLevel == Client.LogLevel.Error && evt.eventId == 1001));
    }

    #endregion

    #region HttpClientFactory configuration

    [DataTestMethod]
    [DataRow(HostKind.Minimal, false)]
    [DataRow(HostKind.Minimal, true)]
    [DataRow(HostKind.Legacy, false)]
    [DataRow(HostKind.Legacy, true)]
    [DataRow(HostKind.PlainDI, false)]
    [DataRow(HostKind.PlainDI, true)]
    public async Task UseHttpClientFactory_ConfiguresConfigFetcher(HostKind hostKind, bool applyFilter)
    {
        const string clientName = "my-client";

        IHost host;

        var sdkKeyDefault = ClientConfigurationHelper.NewSdkKey();
        using var fakeHttpMessageHandlerDefault = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "{ \"f\": { \"testFlagDefault\": { \"t\": 0, \"v\": { \"b\": true } } } }",
            httpETag: new EntityTagHeaderValue("\"123\""));

        var sdkKeyNamed = ClientConfigurationHelper.NewSdkKey();
        using var fakeHttpMessageHandlerNamed = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "{ \"f\": { \"testFlagNamed\": { \"t\": 0, \"v\": { \"b\": true } } } }",
            httpETag: new EntityTagHeaderValue("\"123\""));

        Func<string, bool>? appliesToClient = applyFilter ? (clientName => clientName == Options.DefaultName) : null;

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            Action<ExtendedConfigCatClientOptions, string, string> configureClient = (options, sdkKey, flagName) =>
            {
                options.SdkKey = sdkKey;
                options.FlagOverrides = FlagOverrides.LocalDictionary(new Dictionary<string, object> { [flagName] = false }, OverrideBehaviour.RemoteOverLocal);
            };

            builder
                .AddDefaultClient(options => configureClient(options, sdkKeyDefault, "testFlagDefault"))
                .AddNamedClient(clientName, options => configureClient(options, sdkKeyNamed, "testFlagNamed"))
                .UseHttpClientFactory<Func<FetchRequest, HttpClient>>((factory, request, _) => factory(request), appliesToClient);
        };

        Action<IServiceCollection> setupServices = services =>
        {
            Func<FetchRequest, HttpClient> fakeHttpClientFactory = request =>
            {
                if (request.Uri.AbsolutePath.Contains("/" + sdkKeyDefault + "/"))
                {
                    return new HttpClient(fakeHttpMessageHandlerDefault, disposeHandler: false);
                }
                else if (request.Uri.AbsolutePath.Contains("/" + sdkKeyNamed + "/"))
                {
                    return new HttpClient(fakeHttpMessageHandlerNamed, disposeHandler: false);
                }
                else
                {
                    throw new InvalidOperationException("Unexpected request.");
                }
            };
            services.AddSingleton(fakeHttpClientFactory);
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                setupServices(appBuilder.Services);
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                hostBuilder.ConfigureServices(setupServices);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(setupConfigCatBuilder);
                setupServices(serviceCollection);
                host = new FakeHost(serviceCollection.BuildServiceProvider(),
                    startAsync: (sp, ct) => sp.GetRequiredService<IConfigCatInitializer>().InitializeAsync(ct));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var defaultClient = host.Services.GetRequiredKeyedService<IConfigCatClient>(null);
        var namedClient = host.Services.GetRequiredKeyedService<IConfigCatClient>(clientName);

        Assert.IsTrue(await defaultClient.GetValueAsync("testFlagDefault", (bool?)null));
        Assert.AreEqual(!applyFilter, await namedClient.GetValueAsync("testFlagNamed", (bool?)null));
    }

    [DataTestMethod]
    [DataRow(HostKind.Minimal, false)]
    [DataRow(HostKind.Minimal, true)]
    [DataRow(HostKind.Legacy, false)]
    [DataRow(HostKind.Legacy, true)]
    [DataRow(HostKind.PlainDI, false)]
    [DataRow(HostKind.PlainDI, true)]
    public async Task UseHttpClientFactory_ManualConfigurationTakesPrecedence(HostKind hostKind, bool registerClientFirst)
    {
        const string clientName = "my-client";

        IHost host;

        using var fakeHttpMessageHandlerForFactory = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "{ \"f\": { \"testFlagFactory\": { \"t\": 0, \"v\": { \"b\": true } } } }",
            httpETag: new EntityTagHeaderValue("\"123\""));

        using var fakeHttpMessageHandlerForManualConfiguration = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "{ \"f\": { \"testFlagManual\": { \"t\": 0, \"v\": { \"b\": true } } } }",
            httpETag: new EntityTagHeaderValue("\"123\""));

        using var fakeConfigFetcher = new HttpClientConfigFetcher((_, _) =>
            new HttpClient(fakeHttpMessageHandlerForManualConfiguration, disposeHandler: false));

        Action<ConfigCatBuilder> setupConfigCatBuilder = builder =>
        {
            Action<ExtendedConfigCatClientOptions> configureClient = options =>
            {
                options.SdkKey = ClientConfigurationHelper.NewSdkKey();
                options.ConfigFetcher = fakeConfigFetcher;
            };

            if (!registerClientFirst)
            {
                builder.UseHttpClientFactory<Func<FetchRequest, HttpClient>>((factory, request, _) => factory(request));
            }

            builder
                .AddDefaultClient(configureClient)
                .AddNamedClient(clientName, configureClient);

            if (registerClientFirst)
            {
                builder.UseHttpClientFactory<Func<FetchRequest, HttpClient>>((factory, request, _) => factory(request));
            }
        };

        Action<IServiceCollection> setupServices = services =>
        {
            Func<FetchRequest, HttpClient> fakeHttpClientFactory = request =>
                new HttpClient(fakeHttpMessageHandlerForFactory, disposeHandler: false);
            services.AddSingleton(fakeHttpClientFactory);
        };

        switch (hostKind)
        {
            case HostKind.Minimal:
                var appBuilder = HostFactory.CreateMinimalHostBuilder();
                setupConfigCatBuilder(appBuilder.UseConfigCat());
                setupServices(appBuilder.Services);
                host = appBuilder.Build();
                break;
            case HostKind.Legacy:
                var hostBuilder = HostFactory.CreateLegacyHostBuilder();
                hostBuilder.ConfigureConfigCat(setupConfigCatBuilder);
                hostBuilder.ConfigureServices(setupServices);
                host = hostBuilder.Build();
                break;
            case HostKind.PlainDI:
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddConfigCat(setupConfigCatBuilder);
                setupServices(serviceCollection);
                host = new FakeHost(serviceCollection.BuildServiceProvider(),
                    startAsync: (sp, ct) => sp.GetRequiredService<IConfigCatInitializer>().InitializeAsync(ct));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, null);
        }

        using var _ = host;

        var defaultClient = host.Services.GetRequiredKeyedService<IConfigCatClient>(null);
        var namedClient = host.Services.GetRequiredKeyedService<IConfigCatClient>(clientName);

        Assert.IsNull(await defaultClient.GetValueAsync("testFlagFactory", (bool?)null));
        Assert.IsTrue(await defaultClient.GetValueAsync("testFlagManual", (bool?)null));
        Assert.IsNull(await namedClient.GetValueAsync("testFlagFactory", (bool?)null));
        Assert.IsTrue(await namedClient.GetValueAsync("testFlagManual", (bool?)null));
    }

    #endregion

    #region Argument validation

    [TestMethod]
    public void UseConfigCat_NullBuilder_ThrowsArgumentNullException()
    {
        IHostApplicationBuilder appBuilder = null!;
        Assert.ThrowsException<ArgumentNullException>(() => appBuilder.UseConfigCat());
    }

    [TestMethod]
    public void ConfigureConfigCat_NullBuilder_ThrowsArgumentNullException()
    {
        IHostBuilder builder = null!;
        Assert.ThrowsException<ArgumentNullException>(() => builder.ConfigureConfigCat());
    }

    [TestMethod]
    public void ConfigureConfigCat_NullBuilderWithCallback_ThrowsArgumentNullException()
    {
        IHostBuilder builder = null!;
        Assert.ThrowsException<ArgumentNullException>(() => builder.ConfigureConfigCat(_ => { }));
    }

    [TestMethod]
    public void ConfigureConfigCat_NullBuilderWithContextCallback_ThrowsArgumentNullException()
    {
        IHostBuilder builder = null!;
        Assert.ThrowsException<ArgumentNullException>(() => builder.ConfigureConfigCat((_, _) => { }));
    }

    [TestMethod]
    public void ConfigureConfigCat_NullCallback_ThrowsArgumentNullException()
    {
        var builder = new HostBuilder();
        Assert.ThrowsException<ArgumentNullException>(() => builder.ConfigureConfigCat((Action<ConfigCatBuilder>)null!));
    }

    [TestMethod]
    public void ConfigureConfigCat_NullContextCallback_ThrowsArgumentNullException()
    {
        var builder = new HostBuilder();
        Assert.ThrowsException<ArgumentNullException>(() => builder.ConfigureConfigCat((Action<HostBuilderContext, ConfigCatBuilder>)null!));
    }

    [TestMethod]
    public void AddConfigCat_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        Assert.ThrowsException<ArgumentNullException>(() => services.AddConfigCat(_ => { }));
    }

    [TestMethod]
    public void AddConfigCat_NullServicesWithConfiguration_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var config = new ConfigurationBuilder().Build();
        Assert.ThrowsException<ArgumentNullException>(() => services.AddConfigCat(config));
    }

    [TestMethod]
    public void AddConfigCat_NullServicesWithConfigurationAndCallback_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        var config = new ConfigurationBuilder().Build();
        Assert.ThrowsException<ArgumentNullException>(() => services.AddConfigCat(config, _ => { }));
    }

    [TestMethod]
    public void AddConfigCat_NullCallback_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Assert.ThrowsException<ArgumentNullException>(() => services.AddConfigCat((Action<ConfigCatBuilder>)null!));
    }

    [TestMethod]
    public void AddConfigCat_NullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Assert.ThrowsException<ArgumentNullException>(() => services.AddConfigCat((IConfiguration)null!));
    }

    [TestMethod]
    public void AddConfigCat_NullConfigurationWithCallback_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Assert.ThrowsException<ArgumentNullException>(() => services.AddConfigCat(null!, _ => { }));
    }

    [TestMethod]
    public void AddConfigCat_NullCallbackWithConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        Assert.ThrowsException<ArgumentNullException>(() => services.AddConfigCat(config, null!));
    }

    #endregion

    #region Further public API test cases

    [TestMethod]
    public void UseConfigCat_ReturnsConfigCatBuilderForChaining()
    {
        var appBuilder = HostFactory.CreateMinimalHostBuilder();
        var configCatBuilder = appBuilder.UseConfigCat();

        Assert.IsNotNull(configCatBuilder);
    }

    [TestMethod]
    public void ConfigureConfigCat_WithContextCallback_ContextConfigurationIsAccessible()
    {
        string? capturedEnvName = null;

        using var host = new HostBuilder()
            .ConfigureConfigCat((ctx, b) => capturedEnvName = ctx.HostingEnvironment.EnvironmentName)
            .Build();

        Assert.AreEqual(host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName, capturedEnvName);
    }

    #endregion
}
