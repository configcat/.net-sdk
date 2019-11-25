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
        private const string APIKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

        private static readonly IConfigCatClient client = new ConfigCatClient(APIKEY);

        private static readonly ILogger consoleLogger = new ConsoleLogger(LogLevel.Debug);

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            client?.Dispose();
        }        

        [TestMethod]
        public void ManualPollGetValue()
        {
            IConfigCatClient manualPollClient = ConfigCatClientBuilder
                .Initialize(APIKEY)                
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
                .Initialize(APIKEY)
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
                .Initialize(APIKEY)
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
                .Initialize(APIKEY)
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
                .Initialize(APIKEY)
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
                .Initialize(APIKEY)
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
                .Initialize(APIKEY)
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
    }
}
