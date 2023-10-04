using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;

#if BENCHMARK_OLD
using Config = ConfigCat.Client.SettingsWithPreferences;

namespace ConfigCat.Client.Benchmarks.Old;
#elif BENCHMARK_NEW
namespace ConfigCat.Client.Benchmarks.New;
#else
namespace ConfigCat.Client.Tests;
#endif

// NOTE: These types are intentionally placed into a separate source file because it's also used in the benchmark project.

public interface IMatrixTestDescriptor
{
    public ConfigLocation ConfigLocation { get; }
    public string MatrixResultFileName { get; }
}

public class MatrixTestRunnerBase<TDescriptor> where TDescriptor : IMatrixTestDescriptor, new()
{
    public static readonly TDescriptor DescriptorInstance = new();

    internal readonly Dictionary<string, Setting> config;

    public MatrixTestRunnerBase()
    {
        this.config = DescriptorInstance.ConfigLocation.FetchConfigCached().Settings;
    }

    public static IEnumerable<object?[]> GetTests()
    {
        var resultFilePath = Path.Combine("data", DescriptorInstance.MatrixResultFileName);
        var configLocation = DescriptorInstance.ConfigLocation.ToString();

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
                    configLocation, columns[i], row[i],
                    userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue
                };
            }
        }
    }

    protected virtual bool AssertValue<T>(string expected, Func<string, T> parse, T actual, string keyName, string? userId) => true;

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

    internal int RunAllTests(IRolloutEvaluator evaluator, LoggerWrapper logger, object?[][] tests)
    {
        int i;
        for (i = 0; i < tests.Length; i++)
        {
            var args = tests[i];

            RunTest(evaluator, logger, (string)args[1]!, (string)args[2]!,
                (string?)args[3], (string?)args[4], (string?)args[5], (string?)args[6], (string?)args[7]);
        }
        return i;
    }
}
