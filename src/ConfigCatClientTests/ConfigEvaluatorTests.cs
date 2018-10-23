using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluate;
using ConfigCat.Client.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class ConfigEvaluatorTests
    {
        private static Lazy<string> sampleData = new Lazy<string>(GetSampleV2Json, true);

        private IRolloutEvaluator configEvaluator = new RolloutEvaluator(new NullLogger());

        private ProjectConfig config = new ProjectConfig(sampleData.Value, DateTime.UtcNow, null);

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void GetValue_WithSimpleKey_ShouldReturnCat()
        {            
            string actual = configEvaluator.Evaluate(config, "stringDefaultCat", string.Empty);

            Assert.AreNotEqual(string.Empty, actual);
            Assert.AreEqual("Cat", actual);
        }

        [TestMethod]
        public void GetValue_WithNonExistingKey_ShouldReturnDefaultValue()
        {
            string actual = configEvaluator.Evaluate(config, "NotExistsKey", "NotExistsValue");
            
            Assert.AreEqual("NotExistsValue", actual);
        }

        [TestMethod]
        public void GetValue_WithEmptyProjectConfig_ShouldReturnDefaultValue()
        {
            string actual = configEvaluator.Evaluate(ProjectConfig.Empty, "stringDefaultCat", "Default");
            
            Assert.AreEqual("Default", actual);
        }

        [TestMethod]
        public void GetValue_WithUser_ShouldReturnEvaluatedValue()
        {
            double actual = configEvaluator.Evaluate(config, "doubleDefaultPi", double.NaN, new User("c@configcat.com")
            {
                Email = "c@configcat.com",
                Country = "United Kingdom",
                Custom = new Dictionary<string, string> { { "Custom1", "admin" } }
            });

            Assert.AreEqual(3.1415, actual);
        }

        [TestMethod]
        public void GetValue_WithUserWithIdRules_ShouldReturnEvaluatedValue()
        {
            var actual = configEvaluator.Evaluate(config, "stringUserWithIdentifier", string.Empty, new User("12345"));

            Assert.AreEqual("Cat", actual);
        }

        [TestMethod]
        public void GetValue_WithUserWithOtherIdRules_ShouldReturnEvaluatedValue()
        {
            var actual = configEvaluator.Evaluate(config, "stringUserWithIdentifier", string.Empty, new User("98765"));

            Assert.AreEqual("Dog", actual);
        }


        [TestMethod]
        public async Task GetValue_MatrixTests()
        {
            await MatrixTest(AssertValue);
        }
        
        public static async Task MatrixTest(Action<string, string, User> assertation)
        {
            using (Stream stream = File.OpenRead("testmatrix.csv"))
            using (StreamReader reader = new StreamReader(stream))
            {
                var header = await reader.ReadLineAsync();

                var columns = header.Split(new[] { ';' }).ToList();

                while (!reader.EndOfStream)
                {
                    var rawline = await reader.ReadLineAsync();

                    if (string.IsNullOrEmpty(rawline))
                    {
                        continue;
                    }

                    var row = rawline.Split(new[] { ';' });

                    User u = null;

                    if (row[0] != "##nouserobject##")
                    {
                        u = new User(row[0])
                        {
                            Email = row[1],
                            Country = row[2],
                            Custom = new Dictionary<string, string> { { columns[3], row[3] } }
                        };
                    }

                    for (int i = 4; i < columns.Count; i++)
                    {
                        assertation(columns[i], row[i], u);
                    }
                }
            }
        }

        private void AssertValue(string keyName, string expected, User user)
        {
            var k = keyName.ToLowerInvariant();

            if (k.StartsWith("bool"))
            {
                var actual = configEvaluator.Evaluate(config, keyName, false, user);

                Assert.AreEqual(bool.Parse(expected), actual, $"keyName: {keyName} | userId: {user?.Id}");
            }
            else if (k.StartsWith("double"))
            {
                var actual = configEvaluator.Evaluate(config, keyName, double.NaN, user);

                Assert.AreEqual(double.Parse(expected, CultureInfo.InvariantCulture), actual, $"keyName: {keyName} | userId: {user?.Id}");
            }
            else if (k.StartsWith("integer"))
            {
                var actual = configEvaluator.Evaluate(config, keyName, int.MinValue, user);

                Assert.AreEqual(int.Parse(expected), actual, $"keyName: {keyName} | userId: {user?.Id}");
            }
            else
            {
                var actual = configEvaluator.Evaluate(config, keyName, string.Empty, user);

                Assert.AreEqual(expected, actual, $"keyName: {keyName} | userId: {user?.Id}");
            }
        }

        private static string GetSampleV2Json()
        {
            using (Stream stream = File.OpenRead("sample_v2.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    
}
