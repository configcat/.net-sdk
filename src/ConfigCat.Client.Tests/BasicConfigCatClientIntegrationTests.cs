using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
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
            IConfigCatClient manualPollClient = useNewCreateApi
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
            IConfigCatClient client = useNewCreateApi
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
            IConfigCatClient client = useNewCreateApi
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
            IConfigCatClient client = useNewCreateApi
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
            IConfigCatClient client = useNewCreateApi
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
            IConfigCatClient client = useNewCreateApi
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
            IConfigCatClient manualPollClient = useNewCreateApi
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

            Assert.AreEqual(16, keys.Count());
            Assert.IsTrue(keys.Contains("stringDefaultCat"));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void GetAllValues(bool useNewCreateApi)
        {
            IConfigCatClient manualPollClient = useNewCreateApi
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

            Assert.AreEqual(16, dict.Count());
            Assert.AreEqual("Cat", dict["stringDefaultCat"]);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task GetAllValuesAsync(bool useNewCreateApi)
        {
            IConfigCatClient manualPollClient = useNewCreateApi
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

            Assert.AreEqual(16, dict.Count());
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
            IConfigCatClient manualPollClient = useNewCreateApi
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
            IConfigCatClient manualPollClient = useNewCreateApi
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

            IConfigCatClient manualPollClient = useNewCreateApi
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

            IConfigCatClient manualPollClient = useNewCreateApi
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
    }
}
