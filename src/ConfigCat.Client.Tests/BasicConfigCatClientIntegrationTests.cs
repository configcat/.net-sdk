using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestCategory("Integration")]
[TestClass]
[DoNotParallelize]
public class BasicConfigCatClientIntegrationTests
{
    internal const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

    private static readonly IConfigCatLogger ConsoleLogger = new ConsoleLogger(LogLevel.Debug);
    private static readonly HttpClientHandler SharedHandler = new();

    [TestMethod]
    public void ManualPollGetValue()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        client.ForceRefresh();

        GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [TestMethod]
    public void AutoPollGetValue()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(30));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [TestMethod]
    public void LazyLoadGetValue()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(30));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [TestMethod]
    public async Task ManualPollGetValueAsync()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        await client.ForceRefreshAsync();

        await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [TestMethod]
    public async Task AutoPollGetValueAsync()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(30));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [TestMethod]
    public async Task LazyLoadGetValueAsync()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(30));
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
    }

    [TestMethod]
    public void GetAllKeys()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        client.ForceRefresh();
        var keys = client.GetAllKeys().ToArray();

        Assert.AreEqual(16, keys.Length);
        Assert.IsTrue(keys.Contains("stringDefaultCat"));
    }

    [TestMethod]
    public void GetAllValues()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

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

    [TestMethod]
    public async Task GetAllValuesAsync()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

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

    [TestMethod]
    public void GetAllValueDetails()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        client.ForceRefresh();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var detailsList = client.GetAllValueDetails();

        Assert.AreEqual(16, detailsList.Count);
        var details = detailsList.FirstOrDefault(details => details.Key == "stringDefaultCat");
        Assert.IsNotNull(details);
        Assert.IsFalse(details.IsDefaultValue);
        Assert.AreEqual("Cat", details.Value);
        Assert.AreEqual("7a0be518", details.VariationId);

        CollectionAssert.AreEqual(detailsList.ToArray(), flagEvaluatedEvents.Select(e => e.EvaluationDetails).ToArray());
    }

    [TestMethod]
    public async Task GetAllValueDetailsAsync()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        await client.ForceRefreshAsync();
        var detailsList = await client.GetAllValueDetailsAsync();

        Assert.AreEqual(16, detailsList.Count);
        var details = detailsList.FirstOrDefault(details => details.Key == "stringDefaultCat");
        Assert.IsNotNull(details);
        Assert.IsFalse(details.IsDefaultValue);
        Assert.AreEqual("Cat", details.Value);
        Assert.AreEqual("7a0be518", details.VariationId);

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

    [TestMethod]
    public void GetValueDetailsId()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        client.ForceRefresh();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var actual = client.GetValueDetails("stringDefaultCat", "N/A");

        Assert.IsFalse(actual.IsDefaultValue);
        Assert.AreEqual("Cat", actual.Value);
        Assert.AreEqual("7a0be518", actual.VariationId);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreSame(actual, flagEvaluatedEvents[0].EvaluationDetails);
    }

    [TestMethod]
    public async Task GetValueDetailsAsync()
    {
        static void Configure(ConfigCatClientOptions options)
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = ConsoleLogger;
            options.HttpClientHandler = SharedHandler;
        }

        using IConfigCatClient client = ConfigCatClient.Get(SDKKEY, Configure);

        await client.ForceRefreshAsync();

        var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
        client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

        var actual = await client.GetValueDetailsAsync("stringDefaultCat", "N/A");

        Assert.IsFalse(actual.IsDefaultValue);
        Assert.AreEqual("Cat", actual.Value);
        Assert.AreEqual("7a0be518", actual.VariationId);

        Assert.AreEqual(1, flagEvaluatedEvents.Count);
        Assert.AreSame(actual, flagEvaluatedEvents[0].EvaluationDetails);
    }

    [TestMethod]
    public async Task Http_Timeout_Test_Async()
    {
        var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
        using IConfigCatClient manualPollClient = ConfigCatClient.Get("fake-67890123456789012/1234567890123456789012", options =>
        {
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
        using IConfigCatClient manualPollClient = ConfigCatClient.Get("fake-67890123456789012/1234567890123456789012", options =>
        {
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
        using IConfigCatClient manualPollClient = ConfigCatClient.Get("fake-67890123456789012/1234567890123456789012", options =>
        {
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
        using IConfigCatClient manualPollClient = ConfigCatClient.Get("fake-67890123456789012/1234567890123456789012", options =>
        {
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
        using IConfigCatClient manualPollClient = ConfigCatClient.Get("fake-67890123456789012/1234567890123456789012", options =>
        {
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
        using IConfigCatClient manualPollClient = ConfigCatClient.Get("fake-67890123456789012/1234567890123456789012", options =>
        {
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
        using IConfigCatClient manualPollClient = ConfigCatClient.Get("fake-67890123456789012/1234567890123456789012", options =>
        {
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

    [DataTestMethod]
    [DataRow("specialCharacters", "äöüÄÖÜçéèñışğâ¢™✓😀", "äöüÄÖÜçéèñışğâ¢™✓😀")]
    [DataRow("specialCharactersHashed", "äöüÄÖÜçéèñışğâ¢™✓😀", "äöüÄÖÜçéèñışğâ¢™✓😀")]
    public async Task SpecialCharacters_Works(string settingKey, string userId, string expectedValue)
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dc016a-675e-4aa2-8492-6f572ad98037/244cf8b0-f604-11e8-b543-f23c917f9d8d
        using var client = ConfigCatClient.Get("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g", options =>
        {
            options.PollingMode = PollingModes.LazyLoad();
        });

        var actual = await client.GetValueAsync(settingKey, "NOT_CAT", new User(userId));
        Assert.AreEqual(expectedValue, actual);
    }

    [TestMethod]
    public async Task ShouldIncludeRayIdInLogMessagesWhenHttpResponseIsNotSuccessful()
    {
        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Info);

        using IConfigCatClient client = ConfigCatClient.Get("configcat-sdk-1/~~~~~~~~~~~~~~~~~~~~~~/~~~~~~~~~~~~~~~~~~~~~~", options =>
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.Logger = logger;
        });

        await client.ForceRefreshAsync();

        var errors = logEvents.Where(evt => evt.EventId == 1100).ToArray();
        Assert.AreEqual(1, errors.Length);

        var rayId = errors[0].Message.ArgValues[0] as string;
        Assert.IsNotNull(rayId);
        Assert.AreNotEqual("", rayId);
        Assert.AreNotEqual(LoggerExtensions.FormatRayId(null), rayId);

        StringAssert.Contains(errors[0].Message.InvariantFormattedMessage, rayId);
    }
}
