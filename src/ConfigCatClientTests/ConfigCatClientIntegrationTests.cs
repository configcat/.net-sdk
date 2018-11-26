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
        private static IConfigCatClient client = new ConfigCatClient("PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A");

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
    }
}
