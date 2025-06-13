using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using ConfigCat.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

using JsonObject = JsonElement;
using JsonValue = JsonElement;

[TestClass]
public class EvaluationLogTests
{
    private static readonly string TestDataRootPath = Path.Combine("data", "evaluationlog");

    private static IEnumerable<object?[]> GetSimpleValueTests() => GetTests("simple_value");

    [DataTestMethod]
    [DynamicData(nameof(GetSimpleValueTests), DynamicDataSourceType.Method)]
    public void SimpleValueTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetOneTargetingRuleTests() => GetTests("1_targeting_rule");

    [DataTestMethod]
    [DynamicData(nameof(GetOneTargetingRuleTests), DynamicDataSourceType.Method)]
    public void OneTargetingRuleTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetTwoTargetingRulesTests() => GetTests("2_targeting_rules");

    [DataTestMethod]
    [DynamicData(nameof(GetTwoTargetingRulesTests), DynamicDataSourceType.Method)]
    public void TwoTargetingRulesTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetPercentageOptionsBasedOnUserIdAttributeTests() => GetTests("options_based_on_user_id");

    [DataTestMethod]
    [DynamicData(nameof(GetPercentageOptionsBasedOnUserIdAttributeTests), DynamicDataSourceType.Method)]
    public void PercentageOptionsBasedOnUserIdAttributeTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetPercentageOptionsBasedOnCustomAttributeTests() => GetTests("options_based_on_custom_attr");

    [DataTestMethod]
    [DynamicData(nameof(GetPercentageOptionsBasedOnCustomAttributeTests), DynamicDataSourceType.Method)]
    public void PercentageOptionsBasedOnCustomAttributeTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetPercentageOptionsAfterTargetingRuleTests() => GetTests("options_after_targeting_rule");

    [DataTestMethod]
    [DynamicData(nameof(GetPercentageOptionsAfterTargetingRuleTests), DynamicDataSourceType.Method)]
    public void PercentageOptionsAfterTargetingRuleTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetPercentageOptionsWithinTargetingRuleTests() => GetTests("options_within_targeting_rule");

    [DataTestMethod]
    [DynamicData(nameof(GetPercentageOptionsWithinTargetingRuleTests), DynamicDataSourceType.Method)]
    public void PercentageOptionsWithinTargetingRuleTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetAndRulesTests() => GetTests("and_rules");

    [DataTestMethod]
    [DynamicData(nameof(GetAndRulesTests), DynamicDataSourceType.Method)]
    public void AndRulesTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetSegmentConditionsTests() => GetTests("segment");

    [DataTestMethod]
    [DynamicData(nameof(GetSegmentConditionsTests), DynamicDataSourceType.Method)]
    public void SegmentConditionsTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetPrerequisiteFlagConditionsTests() => GetTests("prerequisite_flag");

    [DataTestMethod]
    [DynamicData(nameof(GetPrerequisiteFlagConditionsTests), DynamicDataSourceType.Method)]
    public void PrerequisiteFlagConditionsTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetComparatorsTests() => GetTests("comparators");

    [DataTestMethod]
    [DynamicData(nameof(GetComparatorsTests), DynamicDataSourceType.Method)]
    public void ComparatorsTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetEpochDateValidationTests() => GetTests("epoch_date_validation");

    [DataTestMethod]
    [DynamicData(nameof(GetEpochDateValidationTests), DynamicDataSourceType.Method)]
    public void EpochDateValidationTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetNumberValidationTests() => GetTests("number_validation");

    [DataTestMethod]
    [DynamicData(nameof(GetNumberValidationTests), DynamicDataSourceType.Method)]
    public void NumberValidationTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetSemVerValidationTests() => GetTests("semver_validation");

    [DataTestMethod]
    [DynamicData(nameof(GetSemVerValidationTests), DynamicDataSourceType.Method)]
    public void SemVerValidationTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetListTruncationTests() => GetTests("list_truncation");

    [DataTestMethod]
    [DynamicData(nameof(GetListTruncationTests), DynamicDataSourceType.Method)]
    public void ListTruncationTests(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName,
        string key, string? defaultValue, string userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        RunTest(testSetName, sdkKey, baseUrlOrOverrideFileName, key, defaultValue, userObject, expectedReturnValue, expectedLogFileName);
    }

    private static IEnumerable<object?[]> GetTests(string testSetName)
    {
        var filePath = Path.Combine(TestDataRootPath, testSetName + ".json");
        var fileContent = File.ReadAllText(filePath);
        var testSet = fileContent.AsSpan().Deserialize<TestSet>();

        foreach (var testCase in testSet!.tests ?? Array.Empty<TestCase>())
        {
            yield return new object?[]
            {
                testSetName,
                testSet.sdkKey,
                testSet.sdkKey is { Length: > 0 } ? testSet.baseUrl : testSet.jsonOverride,
                testCase.key,
                testCase.defaultValue.Serialize(),
                testCase.user?.Serialize(),
                testCase.returnValue.Serialize(),
                testCase.expectedLog
            };
        }
    }

    private static void RunTest(string testSetName, string? sdkKey, string? baseUrlOrOverrideFileName, string key, string? defaultValue, string? userObject, string? expectedReturnValue, string expectedLogFileName)
    {
        var defaultValueParsed = defaultValue?.AsSpan().Deserialize<JsonValue>()!.ToSettingValue(out var settingType).GetValue();
        var expectedReturnValueParsed = expectedReturnValue?.AsSpan().Deserialize<JsonValue>()!.ToSettingValue(out _).GetValue();

        var userObjectParsed = userObject?.AsSpan().Deserialize<Dictionary<string, string>?>();
        User? user;
        if (userObjectParsed is not null)
        {
            user = new User(userObjectParsed[nameof(User.Identifier)]);

            if (userObjectParsed.TryGetValue(nameof(User.Email), out var email))
            {
                user.Email = email;
            }

            if (userObjectParsed.TryGetValue(nameof(User.Country), out var country))
            {
                user.Country = country;
            }

            foreach (var kvp in userObjectParsed)
            {
                if (kvp.Key is not (nameof(User.Identifier) or nameof(User.Email) or nameof(User.Country)))
                {
                    user.Custom[kvp.Key] = kvp.Value;
                }
            }
        }
        else
        {
            user = null;
        }

        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents).AsWrapper();

        ConfigLocation configLocation = sdkKey is { Length: > 0 }
            ? new ConfigLocation.Cdn(sdkKey, baseUrlOrOverrideFileName)
            : new ConfigLocation.LocalFile(TestDataRootPath, "_overrides", baseUrlOrOverrideFileName!);

        var settings = configLocation.FetchConfigCached().SettingsOrEmpty;

        var evaluator = new RolloutEvaluator(logger);
        var evaluationDetails = evaluator.Evaluate(settings, key, defaultValueParsed, user, remoteConfig: null, logger);
        var actualReturnValue = evaluationDetails.Value;

        Assert.AreEqual(expectedReturnValueParsed, actualReturnValue);

        var expectedLogFilePath = Path.Combine(TestDataRootPath, testSetName, expectedLogFileName);
        var expectedLogText = string.Join(Environment.NewLine, File.ReadAllLines(expectedLogFilePath));

        var actualLogText = string.Join(Environment.NewLine, logEvents.Select(evt => FormatLogEvent(ref evt)));

        Assert.AreEqual(expectedLogText, actualLogText);
    }

    private static string FormatLogEvent(ref LogEvent evt)
    {
        var levelString = evt.Level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARNING",
            LogLevel.Error => "ERROR",
            _ => evt.Level.ToString().ToUpperInvariant().PadRight(5)
        };

        var eventIdString = evt.EventId.Id.ToString(CultureInfo.InvariantCulture);

        var exceptionString = evt.Exception is null ? string.Empty : Environment.NewLine + evt.Exception;

        return $"{levelString} [{eventIdString}] {evt.Message.InvariantFormattedMessage}{exceptionString}";
    }

#pragma warning disable IDE1006 // Naming Styles
    public class TestSet
    {
        public string? sdkKey { get; set; }
        public string? baseUrl { get; set; }
        public string? jsonOverride { get; set; }
        public TestCase[]? tests { get; set; }
    }

    public class TestCase
    {
        public string key { get; set; } = null!;
        public JsonValue defaultValue { get; set; } = default!;
        public JsonObject? user { get; set; } = default!;
        public JsonValue returnValue { get; set; } = default!;
        public string expectedLog { get; set; } = null!;
    }
#pragma warning restore IDE1006 // Naming Styles

    [DataTestMethod]
    [DataRow(LogLevel.Off, false)]
    [DataRow(LogLevel.Error, false)]
    [DataRow(LogLevel.Warning, false)]
    [DataRow(LogLevel.Info, true)]
    [DataRow(LogLevel.Debug, true)]
    public void EvaluationLogShouldBeBuiltOnlyWhenNecessary(LogLevel logLevel, bool expectedIsLogBuilt)
    {
        var settings = new ConfigLocation.Cdn("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/AG6C1ngVb0CvM07un6JisQ").FetchConfigCached().SettingsOrEmpty;

        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents, logLevel).AsWrapper();

        var evaluator = new RolloutEvaluator(logger);

        var actualIsLogBuilt = false;
        var evaluatorMock = new Mock<IRolloutEvaluator>();
        evaluatorMock
            .Setup(e => e.Evaluate(It.IsAny<bool?>(), ref It.Ref<EvaluateContext>.IsAny, out It.Ref<bool?>.IsAny))
            .Returns((bool? defaultValue, ref EvaluateContext ctx, out bool? returnValue) =>
            {
                var result = evaluator.Evaluate(defaultValue, ref ctx, out returnValue);
                actualIsLogBuilt = ctx.LogBuilder is not null;
                return result;
            });

        var evaluationResult = evaluatorMock.Object.Evaluate<bool?>(settings, "bool30TrueAdvancedRules", defaultValue: null, user: null, remoteConfig: null, logger);
        Assert.IsFalse(evaluationResult.IsDefaultValue);
        Assert.IsTrue(evaluationResult.Value);

        Assert.AreEqual(actualIsLogBuilt, expectedIsLogBuilt);

        Assert.AreEqual(logLevel >= LogLevel.Warning, logEvents.Any(evt => evt is { Level: LogLevel.Warning, EventId.Id: 3001 }));
        Assert.AreEqual(expectedIsLogBuilt, logEvents.Any(evt => evt is { Level: LogLevel.Info, EventId.Id: 5000 }));
    }
}
