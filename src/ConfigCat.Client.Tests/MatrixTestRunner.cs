using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

public interface IMatrixTestDescriptor
{
    public string SampleJsonFileName { get; }
    public string MatrixResultFileName { get; }
}

public class MatrixTestRunner<TDescriptor> where TDescriptor : IMatrixTestDescriptor, new()
{
    private static readonly Lazy<MatrixTestRunner<TDescriptor>> DefaultLazy = new(() => new MatrixTestRunner<TDescriptor>(), isThreadSafe: true);
    public static MatrixTestRunner<TDescriptor> Default => DefaultLazy.Value;

    public static readonly TDescriptor DescriptorInstance = new();

    private protected readonly Dictionary<string, Setting> config;

    public MatrixTestRunner()
    {
        this.config = ConfigHelper.GetSampleJson(DescriptorInstance.SampleJsonFileName).Deserialize<Config>()!.Settings;
    }

    public static IEnumerable<object?[]> GetTests()
    {
        var resultFilePath = Path.Combine("data", DescriptorInstance.MatrixResultFileName);
        using var reader = new StreamReader(resultFilePath);
        var header = reader.ReadLine()!;

        var columns = header.Split(new[] { ';' });

        while (!reader.EndOfStream)
        {
            var rawline = reader.ReadLine();

            if (string.IsNullOrEmpty(rawline))
                continue;

            var row = rawline.Split(new[] { ';' });

            string? userId = null, userEmail = null, userCountry = null, userCustomAttributeName = null, userCustomAttributeValue = null;
            if (row[0] != "##null##")
            {
                userId = row[0];
                userEmail = row[1] is "" or "##null##" ? null : row[1];
                userCountry = row[2] is "" or "##null##" ? null : row[2];
                if (row[3] is not ("" or "##null##"))
                {
                    userCustomAttributeName = columns[3];
                    userCustomAttributeValue = row[3];
                }
            }

            for (var i = 4; i < columns.Length; i++)
            {
                yield return new[]
                {
                    DescriptorInstance.SampleJsonFileName, columns[i], row[i],
                    userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue
                };
            }
        }
    }

    protected virtual bool AssertValue<T>(string expected, Func<string, T> parse, T actual, string keyName, string? userId)
    {
        Assert.AreEqual(parse(expected), actual, $"jsonFileName: {DescriptorInstance.SampleJsonFileName} | keyName: {keyName} | userId: {userId}");
        return true;
    }

    internal bool RunTest(IRolloutEvaluator evaluator, LoggerWrapper logger, string settingKey, string expectedReturnValue, User? user = null)
    {
        if (settingKey.StartsWith("bool", StringComparison.OrdinalIgnoreCase))
        {
            var actual = evaluator.Evaluate(this.config, settingKey, false, user, null, logger).Value;

            return AssertValue(expectedReturnValue, static e => bool.Parse(e), actual, settingKey, user?.Identifier);
        }
        else if (settingKey.StartsWith("double", StringComparison.OrdinalIgnoreCase))
        {
            var actual = evaluator.Evaluate(this.config, settingKey, double.NaN, user, null, logger).Value;

            return AssertValue(expectedReturnValue, static e => double.Parse(e, CultureInfo.InvariantCulture), actual, settingKey, user?.Identifier);
        }
        else if (settingKey.StartsWith("integer", StringComparison.OrdinalIgnoreCase))
        {
            var actual = evaluator.Evaluate(this.config, settingKey, int.MinValue, user, null, logger).Value;

            return AssertValue(expectedReturnValue, static e => int.Parse(e, CultureInfo.InvariantCulture), actual, settingKey, user?.Identifier);
        }
        else if (settingKey.StartsWith("string", StringComparison.OrdinalIgnoreCase))
        {
            var actual = evaluator.Evaluate(this.config, settingKey, string.Empty, user, null, logger).Value;

            return AssertValue(expectedReturnValue, static e => e, actual, settingKey, user?.Identifier);
        }
        else
        {
            var actual = evaluator.Evaluate(this.config, settingKey, (object?)null, user, null, logger).Value;

            return AssertValue(expectedReturnValue, static e => e, Convert.ToString(actual, CultureInfo.InvariantCulture), settingKey, user?.Identifier);
        }
    }

    internal bool RunTest(IRolloutEvaluator evaluator, LoggerWrapper logger, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        User? user = null;
        if (userId is not null)
        {
            user = new User(userId) { Email = userEmail, Country = userCountry };
            if (userCustomAttributeValue is not null)
                user.Custom[userCustomAttributeName!] = userCustomAttributeValue;
        }

        return RunTest(evaluator, logger, settingKey, expectedReturnValue, user);
    }
}
