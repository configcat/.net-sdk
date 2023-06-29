using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

public interface IMatrixTestDescriptor
{
    public string SampleJsonFileName { get; }
    public string MatrixResultFileName { get; }
}

public abstract class ConfigEvaluatorTestsBase<TDescriptor> where TDescriptor : IMatrixTestDescriptor, new()
{
#pragma warning disable IDE1006 // Naming Styles
    private protected readonly LoggerWrapper Logger;
#pragma warning restore IDE1006 // Naming Styles

    private protected readonly Dictionary<string, Setting> config;

    internal readonly IRolloutEvaluator configEvaluator;

    public ConfigEvaluatorTestsBase()
    {
        var descriptor = new TDescriptor();
        this.config = GetSampleJson(descriptor.SampleJsonFileName).Deserialize<Config>()!.Settings;

        this.Logger = new ConsoleLogger(LogLevel.Debug).AsWrapper();
        this.configEvaluator = new RolloutEvaluator(this.Logger);
    }

    protected virtual void AssertValue(string jsonFileName, string keyName, string expected, User? user)
    {
        var k = keyName.ToLowerInvariant();

        if (k.StartsWith("bool"))
        {
            var actual = this.configEvaluator.Evaluate(this.config, keyName, false, user, null, this.Logger).Value;

            Assert.AreEqual(bool.Parse(expected), actual, $"jsonFileName: {jsonFileName} | keyName: {keyName} | userId: {user?.Identifier}");
        }
        else if (k.StartsWith("double"))
        {
            var actual = this.configEvaluator.Evaluate(this.config, keyName, double.NaN, user, null, this.Logger).Value;

            Assert.AreEqual(double.Parse(expected, CultureInfo.InvariantCulture), actual, $"jsonFileName: {jsonFileName} | keyName: {keyName} | userId: {user?.Identifier}");
        }
        else if (k.StartsWith("integer"))
        {
            var actual = this.configEvaluator.Evaluate(this.config, keyName, int.MinValue, user, null, this.Logger).Value;

            Assert.AreEqual(int.Parse(expected), actual, $"jsonFileName: {jsonFileName} | keyName: {keyName} | userId: {user?.Identifier}");
        }
        else
        {
            var actual = this.configEvaluator.Evaluate(this.config, keyName, string.Empty, user, null, this.Logger).Value;

            Assert.AreEqual(expected, actual, $"jsonFileName: {jsonFileName} | keyName: {keyName} | userId: {user?.Identifier}");
        }
    }

    protected string GetSampleJson(string fileName)
    {
        using Stream stream = File.OpenRead(Path.Combine("data", fileName));
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    public static IEnumerable<object?[]> GetMatrixTests()
    {
        var descriptor = new TDescriptor();

        var resultFilePath = Path.Combine("data", descriptor.MatrixResultFileName);
        using var reader = new StreamReader(resultFilePath);
        var header = reader.ReadLine()!;

        var columns = header.Split(new[] { ';' });

        while (!reader.EndOfStream)
        {
            var rawline = reader.ReadLine();

            if (string.IsNullOrEmpty(rawline))
            {
                continue;
            }

            var row = rawline.Split(new[] { ';' });

            string? userId = null, userEmail = null, userCountry = null, userCustomAttributeName = null, userCustomAttributeValue = null;
            if (row[0] != "##null##")
            {
                userId = row[0];
                userEmail = row[1] == "##null##" ? null : row[1];
                userCountry = row[2] == "##null##" ? null : row[2];
                if (row[3] != "##null##")
                {
                    userCustomAttributeName = columns[3];
                    userCustomAttributeValue = row[3];
                }
            }

            for (var i = 4; i < columns.Length; i++)
            {
                yield return new[]
                {
                    descriptor.SampleJsonFileName, columns[i], row[i],
                    userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue
                };
            }
        }
    }

    [TestCategory("MatrixTests")]
    [DataTestMethod]
    [DynamicData(nameof(GetMatrixTests), DynamicDataSourceType.Method)]
    public void Run_MatrixTests(string jsonFileName, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        User? user = null;
        if (userId is not null)
        {
            user = new User(userId) { Email = userEmail, Country = userCountry };
            if (userCustomAttributeValue is not null)
            {
                user.Custom[userCustomAttributeName!] = userCustomAttributeValue;
            }
        }

        AssertValue(jsonFileName, settingKey, expectedReturnValue, user);
    }
}
