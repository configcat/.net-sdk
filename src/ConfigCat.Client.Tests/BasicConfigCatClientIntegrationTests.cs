using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete
namespace ConfigCat.Client.Tests
{
    [TestCategory("Integration")]
    [TestClass]
    public class BasicConfigCatClientIntegrationTests 
    {
        private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

        private static readonly ILogger consoleLogger = new ConsoleLogger(LogLevel.Debug);

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void ManualPollGetValue(bool useNewCreateApi)
        {
            using IConfigCatClient manualPollClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)                
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            manualPollClient.ForceRefresh();

            GetValueAndAssert(manualPollClient, "stringDefaultCat", "N/A", "Cat");
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void AutoPollGetValue(bool useNewCreateApi)
        {
            using IConfigCatClient client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(30));
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithAutoPoll()
                .WithMaxInitWaitTimeSeconds(30)
                .WithPollIntervalSeconds(600)
                .Create();

            GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void LazyLoadGetValue(bool useNewCreateApi)
        {
            using IConfigCatClient client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(30));
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithLazyLoad()
                .WithCacheTimeToLiveSeconds(30)
                .Create();

            GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task ManualPollGetValueAsync(bool useNewCreateApi)
        {
            using IConfigCatClient client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            await client.ForceRefreshAsync();

            await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task AutoPollGetValueAsync(bool useNewCreateApi)
        {
            using IConfigCatClient client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(30));
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithAutoPoll()
                .WithMaxInitWaitTimeSeconds(30)
                .WithPollIntervalSeconds(600)
                .Create();

            await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task LazyLoadGetValueAsync(bool useNewCreateApi)
        {
            using IConfigCatClient client = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(30));
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithLazyLoad()
                .WithCacheTimeToLiveSeconds(30)
                .Create();

            await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void GetAllKeys(bool useNewCreateApi)
        {
            using IConfigCatClient manualPollClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            manualPollClient.ForceRefresh();
            var keys = manualPollClient.GetAllKeys().ToArray();

            Assert.AreEqual(16, keys.Length);
            Assert.IsTrue(keys.Contains("stringDefaultCat"));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void GetAllValues(bool useNewCreateApi)
        {
            using IConfigCatClient manualPollClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            manualPollClient.ForceRefresh();
            var dict = manualPollClient.GetAllValues();

            Assert.AreEqual(16, dict.Count);
            Assert.AreEqual("Cat", dict["stringDefaultCat"]);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task GetAllValuesAsync(bool useNewCreateApi)
        {
            using IConfigCatClient manualPollClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            await manualPollClient.ForceRefreshAsync();
            var dict = await manualPollClient.GetAllValuesAsync();

            Assert.AreEqual(16, dict.Count);
            Assert.AreEqual("Cat", dict["stringDefaultCat"]);
        }

        private static void GetValueAndAssert(IConfigCatClient client, string key, string defaultValue, string expectedValue)
        {
            var actual = client.GetValue(key, defaultValue);

            Assert.AreEqual(expectedValue, actual);
            Assert.AreNotEqual(defaultValue, actual);
        }

        private static async Task GetValueAsyncAndAssert(IConfigCatClient client, string key, string defaultValue, string expectedValue)
        {
            var actual = await client.GetValueAsync(key, defaultValue);

            Assert.AreEqual(expectedValue, actual);
            Assert.AreNotEqual(defaultValue, actual);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void GetVariationId(bool useNewCreateApi)
        {
            using IConfigCatClient manualPollClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            manualPollClient.ForceRefresh();
            var actual = manualPollClient.GetVariationId("stringDefaultCat", "default");

            Assert.AreEqual("7a0be518", actual);            
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task GetVariationIdAsync(bool useNewCreateApi)
        {
            using IConfigCatClient manualPollClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            await manualPollClient.ForceRefreshAsync();

            var actual = await manualPollClient.GetVariationIdAsync("stringDefaultCat", "default");

            Assert.AreEqual("7a0be518", actual);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void GetAllVariationId(bool useNewCreateApi)
        {
            // Arrange

            const string expectedJsonString = "[\"7a0be518\",\"83372510\",\"2459598d\",\"ce564c3a\",\"44ab483a\",\"d227b334\",\"93f5a1c0\",\"bb66b1f3\",\"09513143\",\"489a16d2\",\"607147d5\",\"11634414\",\"faadbf54\",\"5af8acc7\",\"183ee713\",\"baff2362\"]";

            var expectedValue = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(expectedJsonString);

            using IConfigCatClient manualPollClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            manualPollClient.ForceRefresh();

            // Act

            var actual = manualPollClient.GetAllVariationId(new User("a@configcat.com"));

            // Assert            
            Assert.AreEqual(16, expectedValue.Length);
            CollectionAssert.AreEquivalent(expectedValue, actual.ToArray());
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task GetAllVariationIdAsync(bool useNewCreateApi)
        {
            // Arrange

            const string expectedJsonString = "[\"7a0be518\",\"83372510\",\"2459598d\",\"ce564c3a\",\"44ab483a\",\"d227b334\",\"93f5a1c0\",\"bb66b1f3\",\"09513143\",\"489a16d2\",\"607147d5\",\"11634414\",\"faadbf54\",\"5af8acc7\",\"183ee713\",\"baff2362\"]";

            var expectedValue = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(expectedJsonString);

            using IConfigCatClient manualPollClient = useNewCreateApi
                ? new ConfigCatClient(options =>
                {
                    options.SdkKey = SDKKEY;
                    options.PollingMode = PollingModes.ManualPoll;
                    options.Logger = consoleLogger;
                })
                : ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            await manualPollClient.ForceRefreshAsync();

            // Act

            var actual = await manualPollClient.GetAllVariationIdAsync(new User("a@configcat.com"));

            // Assert            
            Assert.AreEqual(16, expectedValue.Length);
            CollectionAssert.AreEquivalent(expectedValue, actual.ToArray());
        }

        [TestMethod]
        public async Task Http_Timeout_Test_Async()
        {
            var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
            using IConfigCatClient manualPollClient = new ConfigCatClient(options =>
            {
                options.SdkKey = "fake";
                options.PollingMode = PollingModes.ManualPoll;
                options.Logger = consoleLogger;
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
                options.Logger = consoleLogger;
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
                options.Logger = consoleLogger;
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
                options.Logger = consoleLogger;
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
                options.Logger = consoleLogger;
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
                options.Logger = consoleLogger;
                options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response, TimeSpan.FromSeconds(5));
            });

            Task.Run(() => 
            {
                manualPollClient.ForceRefresh();
                defer.Set();
            });
            manualPollClient.Dispose();
            defer.WaitOne();

            Assert.IsTrue(DateTimeOffset.UtcNow.Subtract(now) < TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void Ensure_Multiple_Requests_Doesnt_Interfere_In_ValueTasks()
        {
            var response = $"{{ \"f\": {{ \"fakeKey\": {{ \"v\": \"fakeValue\", \"p\": [] ,\"r\": [] }} }} }}";
            IConfigCatClient manualPollClient = new ConfigCatClient(options =>
            {
                options.SdkKey = "fake";
                options.Logger = consoleLogger;
                options.PollingMode = PollingModes.ManualPoll;
                options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, response);
            });

            // an exception should be thrown when the value task's result is fetched without completion.
            Parallel.For(0, 10, _ =>
            {
                manualPollClient.ForceRefresh();
            });
        }
    }
}
