using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

public abstract class ConfigEvaluatorTestsBase
{
#pragma warning disable IDE1006 // Naming Styles
    private protected readonly LoggerWrapper Logger = new ConsoleLogger(LogLevel.Debug).AsWrapper();
#pragma warning restore IDE1006 // Naming Styles

    private protected readonly Dictionary<string, Setting> config;

    internal readonly IRolloutEvaluator configEvaluator;

    protected abstract string SampleJsonFileName { get; }

    protected abstract string MatrixResultFileName { get; }

    public ConfigEvaluatorTestsBase()
    {
        this.configEvaluator = new RolloutEvaluator(this.Logger);

        this.config = GetSampleJson().Deserialize<Config>()!.Settings;
    }

    protected virtual void AssertValue(string keyName, string expected, User? user)
    {
        var k = keyName.ToLowerInvariant();

        if (k.StartsWith("bool"))
        {
            var actual = this.configEvaluator.Evaluate(this.config, keyName, false, user, null, this.Logger).Value;

            Assert.AreEqual(bool.Parse(expected), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
        }
        else if (k.StartsWith("double"))
        {
            var actual = this.configEvaluator.Evaluate(this.config, keyName, double.NaN, user, null, this.Logger).Value;

            Assert.AreEqual(double.Parse(expected, CultureInfo.InvariantCulture), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
        }
        else if (k.StartsWith("integer"))
        {
            var actual = this.configEvaluator.Evaluate(this.config, keyName, int.MinValue, user, null, this.Logger).Value;

            Assert.AreEqual(int.Parse(expected), actual, $"keyName: {keyName} | userId: {user?.Identifier}");
        }
        else
        {
            var actual = this.configEvaluator.Evaluate(this.config, keyName, string.Empty, user, null, this.Logger).Value;

            Assert.AreEqual(expected, actual, $"keyName: {keyName} | userId: {user?.Identifier}");
        }
    }

    protected string GetSampleJson()
    {
        using Stream stream = File.OpenRead(Path.Combine("data", SampleJsonFileName));
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    public async Task MatrixTest(Action<string, string, User?> assertation)
    {
        using Stream stream = File.OpenRead(Path.Combine("data", MatrixResultFileName));
        using StreamReader reader = new(stream);
        var header = (await reader.ReadLineAsync())!;

        var columns = header.Split(new[] { ';' }).ToList();

        while (!reader.EndOfStream)
        {
            var rawline = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(rawline))
            {
                continue;
            }

            var row = rawline.Split(new[] { ';' });

            User? u = null;

            if (row[0] != "##null##")
            {
                u = new User(row[0])
                {
                    Email = row[1] == "##null##" ? null : row[1],
                    Country = row[2] == "##null##" ? null : row[2],
                    Custom = row[3] == "##null##" ? null! : new Dictionary<string, string?> { { columns[3], row[3] } }
                };
            }

            for (var i = 4; i < columns.Count; i++)
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
