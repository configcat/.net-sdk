using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#pragma warning disable CS0618 // Type or member is obsolete
namespace ConfigCat.Client.Tests;

[TestCategory("Integration")]
[TestClass]
[DoNotParallelize]
public class BasicConfigCatClientIntegrationTests
{
    internal const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

    private static readonly ILogger ConsoleLogger = new ConsoleLogger(LogLevel.Debug);
    private static readonly HttpClientHandler SharedHandler = new();

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void ManualPollGetValue(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        client.ForceRefresh();

        GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void AutoPollGetValue(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(30));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithAutoPoll()
                .WithMaxInitWaitTimeSeconds(30)
                .WithPollIntervalSeconds(600)
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void LazyLoadGetValue(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(30));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithLazyLoad()
                .WithCacheTimeToLiveSeconds(30)
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public async Task ManualPollGetValueAsync(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        await client.ForceRefreshAsync();

        await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public async Task AutoPollGetValueAsync(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(30));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithAutoPoll()
                .WithMaxInitWaitTimeSeconds(30)
                .WithPollIntervalSeconds(600)
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public async Task LazyLoadGetValueAsync(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(30));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithLazyLoad()
                .WithCacheTimeToLiveSeconds(30)
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void GetAllKeys(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        client.ForceRefresh();
        var keys = client.GetAllKeys().ToArray();

        Assert.AreEqual(16, keys.Length);
        Assert.IsTrue(keys.Contains("stringDefaultCat"));
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void GetAllValues(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        client.ForceRefresh();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var dict = client.GetAllValues();

        Assert.AreEqual(16, dict.Count);
        Assert.AreEqual("Cat", dict["stringDefaultCat"]);

        Assert.AreEqual(16, flagEvaluatedEvents.Count);
        var evaluationDetails = flagEvaluatedEvents.ToDictionary(e => e.EvaluationDetails.Key, e => e.EvaluationDetails);
        foreach (var entry in dict)
        {
            Assert.IsTrue(evaluationDetails.TryGetValue(entry.Key, out var evaluationDetail));
            Assert.AreEqual(entry.Value, evaluationDetail.Value);
            Assert.IsFalse(evaluationDetail.IsDefaultValue);
        }
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public async Task GetAllValuesAsync(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        await client.ForceRefreshAsync();
        var dict = await client.GetAllValuesAsync();

        Assert.AreEqual(16, dict.Count);
        Assert.AreEqual("Cat", dict["stringDefaultCat"]);

        Assert.AreEqual(16, flagEvaluatedEvents.Count);
        var evaluationDetails = flagEvaluatedEvents.ToDictionary(e => e.EvaluationDetails.Key, e => e.EvaluationDetails);
        foreach (var entry in dict)
        {
            Assert.IsTrue(evaluationDetails.TryGetValue(entry.Key, out var evaluationDetail));
            Assert.AreEqual(entry.Value, evaluationDetail.Value);
            Assert.IsFalse(evaluationDetail.IsDefaultValue);
        }
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void GetAllValueDetails(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        client.ForceRefresh();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var detailsList = client.GetAllValueDetails();

        Assert.AreEqual(16, detailsList.Count);
        var details = detailsList.FirstOrDefault(details => details.Key == "stringDefaultCat");
        Assert.IsNotNull(details);
        Assert.AreEqual("Cat", details.Value);
        Assert.IsFalse(details.IsDefaultValue);

        CollectionAssert.AreEqual(detailsList.ToArray(), flagEvaluatedEvents.Select(e => e.EvaluationDetails).ToArray());
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public async Task GetAllValueDetailsAsync(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        await client.ForceRefreshAsync();
        var detailsList = await client.GetAllValueDetailsAsync();

        Assert.AreEqual(16, detailsList.Count);
        var details = detailsList.FirstOrDefault(details => details.Key == "stringDefaultCat");
        Assert.IsNotNull(details);
        Assert.AreEqual("Cat", details.Value);
        Assert.IsFalse(details.IsDefaultValue);

        CollectionAssert.AreEqual(detailsList.ToArray(), flagEvaluatedEvents.Select(e => e.EvaluationDetails).ToArray());
    }

    private static void GetValueAndAssert(IConfigCatClient client, string key, string defaultValue, string expectedValue)
    {
        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var actual = client.GetValue(key, defaultValue);

        Assert.AreEqual(expectedValue, actual);
        Assert.AreNotEqual(defaultValue, actual);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreEqual(expectedValue, flagEvaluatedEvents[0].EvaluationDetails.Value);
        Assert.IsFalse(flagEvaluatedEvents[0].EvaluationDetails.IsDefaultValue);
    }

    private static async Task GetValueAsyncAndAssert(IConfigCatClient client, string key, string defaultValue, string expectedValue)
    {
        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var actual = await client.GetValueAsync(key, defaultValue);

        Assert.AreEqual(expectedValue, actual);
        Assert.AreNotEqual(defaultValue, actual);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreEqual(expectedValue, flagEvaluatedEvents[0].EvaluationDetails.Value);
        Assert.IsFalse(flagEvaluatedEvents[0].EvaluationDetails.IsDefaultValue);
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void GetValueDetailsId(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        client.ForceRefresh();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var actual = client.GetValueDetails("stringDefaultCat", "N/A");

        Assert.AreEqual("Cat", actual.Value);
        Assert.IsFalse(actual.IsDefaultValue);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreSame(actual, flagEvaluatedEvents[0].EvaluationDetails);
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public async Task GetValueDetailsAsync(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        await client.ForceRefreshAsync();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var actual = await client.GetValueDetailsAsync("stringDefaultCat", "N/A");

        Assert.AreEqual("Cat", actual.Value);
        Assert.IsFalse(actual.IsDefaultValue);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreSame(actual, flagEvaluatedEvents[0].EvaluationDetails);
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void GetVariationId(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        client.ForceRefresh();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var actual = client.GetVariationId("stringDefaultCat", "default");

        Assert.AreEqual("7a0be518", actual);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreEqual("7a0be518", flagEvaluatedEvents[0].EvaluationDetails.VariationId);
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public async Task GetVariationIdAsync(ClientCreationStrategy creationStrategy)
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        await client.ForceRefreshAsync();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var actual = await client.GetVariationIdAsync("stringDefaultCat", "default");

        Assert.AreEqual("7a0be518", actual);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreEqual("7a0be518", flagEvaluatedEvents[0].EvaluationDetails.VariationId);
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public void GetAllVariationId(ClientCreationStrategy creationStrategy)
    {
        // Arrange

        const string expectedJsonString = "[\"7a0be518\",\"83372510\",\"2459598d\",\"ce564c3a\",\"44ab483a\",\"d227b334\",\"93f5a1c0\",\"bb66b1f3\",\"09513143\",\"489a16d2\",\"607147d5\",\"11634414\",\"faadbf54\",\"5af8acc7\",\"183ee713\",\"baff2362\"]";

        var expectedValue = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(expectedJsonString);

        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        client.ForceRefresh();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        // Act

        var actual = client.GetAllVariationId(new User("a@configcat.com"));

        // Assert            
        Assert.AreEqual(16, expectedValue.Length);
        CollectionAssert.AreEquivalent(expectedValue, actual.ToArray());

        CollectionAssert.AreEquivalent(expectedValue, flagEvaluatedEvents.Select(e => e.EvaluationDetails.VariationId).ToArray());
    }

    [DataRow(ClientCreationStrategy.Singleton)]
    [DataRow(ClientCreationStrategy.Constructor)]
    [DataRow(ClientCreationStrategy.Builder)]
    [DataTestMethod]
    public async Task GetAllVariationIdAsync(ClientCreationStrategy creationStrategy)
    {
        // Arrange

        const string expectedJsonString = "[\"7a0be518\",\"83372510\",\"2459598d\",\"ce564c3a\",\"44ab483a\",\"d227b334\",\"93f5a1c0\",\"bb66b1f3\",\"09513143\",\"489a16d2\",\"607147d5\",\"11634414\",\"faadbf54\",\"5af8acc7\",\"183ee713\",\"baff2362\"]";

        var expectedValue = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(expectedJsonString);

        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = creationStrategy switch
        {
            ClientCreationStrategy.Singleton => ConfigCatClient.Get(SDKKEY, Configure),
            ClientCreationStrategy.Constructor => new ConfigCatClient(options => { Configure(options); options.SdkKey = SDKKEY; }),
            ClientCreationStrategy.Builder => ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(ConsoleLogger)
                .WithManualPoll()
                .WithHttpClientHandler(SharedHandler)
                .Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(creationStrategy))
        };

        await client.ForceRefreshAsync();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        // Act

        var actual = await client.GetAllVariationIdAsync(new User("a@configcat.com"));

        // Assert            
        Assert.AreEqual(16, expectedValue.Length);
        CollectionAssert.AreEquivalent(expectedValue, actual.ToArray());

        CollectionAssert.AreEquivalent(expectedValue, flagEvaluatedEvents.Select(e => e.EvaluationDetails.VariationId).ToArray());
    }

    [TestMethod]
    public async Task Http_Timeout_Test_Async()
    {
        var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
        using IConfigCatClient manualPollClient = new ConfigCatClient(options =>
        {
            options.SdkKey = "fake";
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpTimeout = TimeSpan.FromSeconds(0.5);
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response, TimeSpan.FromSeconds(5));
        });

        await manualPollClient.ForceRefreshAsync();

        Assert.AreEqual(string.Empty, await manualPollClient.GetValueAsync("fakeKey", string.Empty));
    }

    [TestMethod]
    public void Http_Timeout_Test_Sync()
    {
        var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
        using IConfigCatClient manualPollClient = new ConfigCatClient(options =>
        {
            options.SdkKey = "fake";
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpTimeout = TimeSpan.FromSeconds(0.5);
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response, TimeSpan.FromSeconds(5));
        });

        manualPollClient.ForceRefresh();
        Assert.AreEqual(string.Empty, manualPollClient.GetValue("fakeKey", string.Empty));
    }

    [TestMethod]
    public async Task Ensure_MaxInitWait_Overrides_Timeout()
    {
        var now = DateTimeOffset.UtcNow;
        var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
        using IConfigCatClient manualPollClient = new ConfigCatClient(options =>
        {
            options.SdkKey = "fake";
            options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(1));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response, TimeSpan.FromSeconds(5));
        });

        Assert.AreEqual(string.Empty, await manualPollClient.GetValueAsync("fakeKey", string.Empty));
        Assert.IsTrue(DateTimeOffset.UtcNow.Subtract(now) < TimeSpan.FromSeconds(1.5));
    }

    [TestMethod]
    public void Ensure_MaxInitWait_Overrides_Timeout_Sync()
    {
        var now = DateTimeOffset.UtcNow;
        var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
        using IConfigCatClient manualPollClient = new ConfigCatClient(options =>
        {
            options.SdkKey = "fake";
            options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.FromSeconds(1));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response, TimeSpan.FromSeconds(5));
        });

        Assert.AreEqual(string.Empty, manualPollClient.GetValue("fakeKey", string.Empty));
        Assert.IsTrue(DateTimeOffset.UtcNow.Subtract(now) < TimeSpan.FromSeconds(1.5));
    }

    [TestMethod]
    public void Ensure_Client_Dispose_Kill_Hanging_Http_Call()
    {
        var defer = new ManualResetEvent(false);
        var now = DateTimeOffset.UtcNow;
        var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
        IConfigCatClient manualPollClient = new ConfigCatClient(options =>
        {
            options.SdkKey = "fake";
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response, TimeSpan.FromSeconds(5));
        });

        manualPollClient.ForceRefreshAsync().ContinueWith(_ => defer.Set());
        manualPollClient.Dispose();
        defer.WaitOne();

        Assert.IsTrue(DateTimeOffset.UtcNow.Subtract(now) < TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Ensure_Client_Dispose_Kill_Hanging_Http_Call_Sync()
    {
        var defer = new ManualResetEvent(false);
        var now = DateTimeOffset.UtcNow;
        var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
        IConfigCatClient manualPollClient = new ConfigCatClient(options =>
        {
            options.SdkKey = "fake";
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response, TimeSpan.FromSeconds(5));
        });

        Task.Run(() =>
        {
            manualPollClient.ForceRefresh();
            defer.Set();
        });
        manualPollClient.Dispose();
        defer.WaitOne();

        Assert.IsTrue(DateTimeOffset.UtcNow.Subtract(now) < TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void Ensure_Multiple_Requests_Doesnt_Interfere_In_ValueTasks()
    {
        var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
        IConfigCatClient manualPollClient = new ConfigCatClient(options =>
        {
            options.SdkKey = "fake";
            options.Logger = ConsoleLogger;
            options.PollingMode = PollingModes.ManualPoll;
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response);
        });

        // an exception should be thrown when the value task's result is fetched without completion.
        Parallel.For(0, 10, _ =>
        {
            manualPollClient.ForceRefresh();
        });
    }

    public enum ClientCreationStrategy
    {
        Singleton,
        Constructor,
        Builder,
    }
}
