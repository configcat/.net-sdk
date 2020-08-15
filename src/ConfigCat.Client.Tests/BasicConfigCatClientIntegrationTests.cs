using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;


namespace ConfigCat.Client.Tests
{
    [TestCategory("Integration")]
    [TestClass]
    public class BasicConfigCatClientIntegrationTests 
    {
        private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

        private static readonly ILogger consoleLogger = new ConsoleLogger(LogLevel.Debug);

        [TestMethod]
        public void ManualPollGetValue()
        {
            IConfigCatClient manualPollClient = ConfigCatClientBuilder
                .Initialize(SDKKEY)                
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            manualPollClient.ForceRefresh();

            GetValueAndAssert(manualPollClient, "stringDefaultCat", "N/A", "Cat");
        }

        [TestMethod]
        public void AutoPollGetValue()
        {
            IConfigCatClient client = ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithAutoPoll()
                .WithMaxInitWaitTimeSeconds(30)
                .WithPollIntervalSeconds(600)
                .Create();

            GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [TestMethod]
        public void LazyLoadGetValue()
        {
            IConfigCatClient client = ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithLazyLoad()
                .WithCacheTimeToLiveSeconds(30)
                .Create();

            GetValueAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [TestMethod]
        public async Task ManualPollGetValueAsync()
        {
            IConfigCatClient client = ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            await client.ForceRefreshAsync();

            await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [TestMethod]
        public async Task AutoPollGetValueAsync()
        {
            IConfigCatClient client = ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithAutoPoll()
                .WithMaxInitWaitTimeSeconds(30)
                .WithPollIntervalSeconds(600)
                .Create();

            await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [TestMethod]
        public async Task LazyLoadGetValueAsync()
        {
            IConfigCatClient client = ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithLazyLoad()
                .WithCacheTimeToLiveSeconds(30)
                .Create();

            await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
        }

        [TestMethod]
        public void GetAllKeys()
        {
            IConfigCatClient manualPollClient = ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            manualPollClient.ForceRefresh();
            var keys = manualPollClient.GetAllKeys().ToArray();

            Assert.AreEqual(16, keys.Count());
            Assert.IsTrue(keys.Contains("stringDefaultCat"));
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

        [TestMethod]
        public void GetVariationId()
        {
            IConfigCatClient manualPollClient = ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            manualPollClient.ForceRefresh();
            var actual = manualPollClient.GetVariationId("stringDefaultCat", "default");

            Assert.AreEqual("7a0be518", actual);            
        }

        [TestMethod]
        public async Task GetVariationIdAsync()
        {
            IConfigCatClient manualPollClient = ConfigCatClientBuilder
                .Initialize(SDKKEY)
                .WithLogger(consoleLogger)
                .WithManualPoll()
                .Create();

            await manualPollClient.ForceRefreshAsync();

            var actual = await manualPollClient.GetVariationIdAsync("stringDefaultCat", "default");

            Assert.AreEqual("7a0be518", actual);
        }

        [TestMethod]
        public void GetAllVariationId()
        {
            // Arrange

            const string expectedJsonString = "[\"7a0be518\",\"83372510\",\"2459598d\",\"ce564c3a\",\"44ab483a\",\"d227b334\",\"93f5a1c0\",\"bb66b1f3\",\"09513143\",\"489a16d2\",\"607147d5\",\"11634414\",\"faadbf54\",\"5af8acc7\",\"183ee713\",\"baff2362\"]";

            var expectedValue = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(expectedJsonString);

            IConfigCatClient manualPollClient = ConfigCatClientBuilder
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

        [TestMethod]
        public async Task GetAllVariationIdAsync()
        {
            // Arrange

            const string expectedJsonString = "[\"7a0be518\",\"83372510\",\"2459598d\",\"ce564c3a\",\"44ab483a\",\"d227b334\",\"93f5a1c0\",\"bb66b1f3\",\"09513143\",\"489a16d2\",\"607147d5\",\"11634414\",\"faadbf54\",\"5af8acc7\",\"183ee713\",\"baff2362\"]";

            var expectedValue = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(expectedJsonString);

            IConfigCatClient manualPollClient = ConfigCatClientBuilder
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
