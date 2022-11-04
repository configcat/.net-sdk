using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
    public abstract class ConfigEvaluatorTestsBase
    {
        protected readonly ILogger logger = new ConsoleLogger(LogLevel.Debug);

        private protected readonly IDictionary<string, Setting> config;

        internal readonly IRolloutEvaluator configEvaluator;

        protected abstract string SampleJsonFileName { get; }

        protected abstract string MatrixResultFileName { get; }

        public ConfigEvaluatorTestsBase()
        {
            this.configEvaluator = new RolloutEvaluator(new LoggerWrapper(logger));

            this.config = this.GetSampleJson().Deserialize<SettingsWithPreferences>().Settings;
        }

        protected virtual void AssertValue(string keyName, string expected, User user)
        {
            var k = keyName.ToLowerInvariant();

            if (k.StartsWith("bool"))
            {
                var actual = configEvaluator.Evaluate(config, keyName, false, user, null, logger, out _);

                Assert.AreEqual(bool.Parse(expected), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
            }
            else if (k.StartsWith("double"))
            {
                var actual = configEvaluator.Evaluate(config, keyName, double.NaN, user, null, logger, out _);

                Assert.AreEqual(double.Parse(expected, CultureInfo.InvariantCulture), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
            }
            else if (k.StartsWith("integer"))
            {
                var actual = configEvaluator.Evaluate(config, keyName, int.MinValue, user, null, logger, out _);

                Assert.AreEqual(int.Parse(expected), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
            }
            else
            {
                var actual = configEvaluator.Evaluate(config, keyName, string.Empty, user, null, logger, out _);

                Assert.AreEqual(expected, actual, $"keyName: {keyName} | userId: {user?.Identifier}");
            }
        }

        protected string GetSampleJson()
        {
            using Stream stream = File.OpenRead("data" + Path.DirectorySeparatorChar + this.SampleJsonFileName);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        public async Task MatrixTest(Action<string, string, User> assertation)
        {
            using Stream stream = File.OpenRead("data" + Path.DirectorySeparatorChar + this.MatrixResultFileName);
            using StreamReader reader = new(stream);
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

                if (row[0] != "##null##")
                {
                    u = new User(row[0])
                    {
                        Email = row[1] == "##null##" ? null : row[1],
                        Country = row[2] == "##null##" ? null : row[2],
                        Custom = row[3] == "##null##" ? null : new Dictionary<string, string> { { columns[3], row[3] } }
                    };
                }

                for (int i = 4; i < columns.Count; i++)
                {
                    assertation(columns[i], row[i], u);
                }
            }
        }

        [TestCategory("MatrixTests")]
        [TestMethod]
        public async Task Run_MatrixTests()
        {
            await MatrixTest(AssertValue);
        }       
    }
}
