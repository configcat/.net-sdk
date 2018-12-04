using System;
using System.Globalization;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluate;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace ConfigCat.Client.Tests
{
    [TestCategory("Integration")]
    [TestClass]
    public class ConfigCatClientIntegrationTests
    {
        private const string APIKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

        private static IConfigCatClient client = new ConfigCatClient(APIKEY);

        [TestMethod]
        public async Task GetValue_MatrixTests()
        {
            await ConfigEvaluatorTests.MatrixTest(AssertValue);
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            client?.Dispose();
        }

        private void AssertValue(string keyName, string expected, User user)
        {
            var k = keyName.ToLowerInvariant();

            if (k.StartsWith("bool"))
            {
                var actual = client.GetValue(keyName, false, user);

                Assert.AreEqual(bool.Parse(expected), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
            }
            else if (k.StartsWith("double"))
            {
                var actual = client.GetValue(keyName, double.NaN, user);

                Assert.AreEqual(double.Parse(expected, CultureInfo.InvariantCulture), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
            }
            else if (k.StartsWith("integer"))
            {
                var actual = client.GetValue(keyName, int.MaxValue, user);

                Assert.AreEqual(int.Parse(expected), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
            }
            else
            {
                var actual = client.GetValue(keyName, string.Empty, user);

                Assert.AreEqual(expected, actual, $"keyName: {keyName} | userId: {user?.Identifier}");
            }
        }

        [TestMethod]
        public void ManualPollGetValue()
        {
            IConfigCatClient manualPollClient = ConfigCatClientBuilder
                .Initialize(APIKEY)
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
                .WithLazyLoad()
                .WithCacheTimeToLiveSeconds(30)
                .Create();

            await GetValueAsyncAndAssert(client, "stringDefaultCat", "N/A", "Cat");
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
