using System;
using System.IO;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using ConfigCat.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ModelTests
{
    [DataTestMethod]
    [DataRow(false, "False")]
    [DataRow(true, "True")]
    [DataRow("Text", "Text")]
    [DataRow(1, "1")]
    [DataRow(1L, "1")]
    [DataRow(1d, "1")]
    [DataRow(3.14, "3.14")]
    [DataRow(null, EvaluateLogHelper.InvalidValuePlaceholder)]
    public void SettingValue_ToString(object? value, string expectedResult)
    {
        var settingValue = value.ToSettingValue(out _);
        var actualResult = settingValue.ToString();
        Assert.AreEqual(expectedResult, actualResult);
    }

    [DataTestMethod]
    [DataRow("sample_v5", "stringIsNotInDogDefaultCat", 0, 0, new[] { "User.Email IS NOT ONE OF (hashed) [<2 hashed values>]" })]
    [DataRow("sample_segments_v6", "countrySegment", 0, 0, new[] { "User IS IN SEGMENT 'United'" })]
    [DataRow("sample_flagdependency_v6", "boolDependsOnBool", 0, 0, new[] { "Flag 'mainBoolFlag' EQUALS 'True'" })]
    public void Condition_ToString(string configJsonFileName, string settingKey, int targetingRuleIndex, int conditionIndex, string[] expectedResultLines)
    {
        var pc = ConfigHelper.FromFile(Path.Combine("data", configJsonFileName + ".json"), null, default);
        IConfig config = pc.Config!;
        var setting = config.Settings[settingKey];
        var targetingRule = setting.TargetingRules[targetingRuleIndex];
        var condition = targetingRule.Conditions[conditionIndex];
        var actualResult = condition!.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }

    [DataTestMethod]
    [DataRow("sample_v5", "string25Cat25Dog25Falcon25Horse", -1, 0, new[] { "25%: 'Cat'" })]
    [DataRow("sample_comparators_v6", "missingPercentageAttribute", 0, 0, new[] { "50%: 'Falcon'" })]
    public void PercentageOption_ToString(string configJsonFileName, string settingKey, int targetingRuleIndex, int percentageOptionIndex, string[] expectedResultLines)
    {
        var pc = ConfigHelper.FromFile(Path.Combine("data", configJsonFileName + ".json"), null, default);
        IConfig config = pc.Config!;
        var setting = config.Settings[settingKey];
        var percentageOptions = targetingRuleIndex >= 0
            ? setting.TargetingRules[targetingRuleIndex].PercentageOptions
            : setting.PercentageOptions;
        IPercentageOption percentageOption = percentageOptions![percentageOptionIndex];
        var actualResult = percentageOption!.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }

    [DataTestMethod]
    [DataRow("sample_v5", "stringIsNotInDogDefaultCat", 0, new[]
    {
        "IF User.Email IS NOT ONE OF (hashed) [<2 hashed values>]",
        "THEN 'Dog'",
    })]
    [DataRow("sample_comparators_v6", "missingPercentageAttribute", 0, new[]
    {
        "IF User.Email ENDS WITH ANY OF (hashed) [<1 hashed value>]",
        "THEN",
        "  50%: 'Falcon'",
        "  50%: 'Horse'",
    })]
    [DataRow("sample_and_or_v6", "emailAnd", 0, new[]
    {
        "IF User.Email STARTS WITH ANY OF (hashed) [<1 hashed value>]",
        "  AND User.Email CONTAINS ANY OF ['@']",
        "  AND User.Email ENDS WITH ANY OF (hashed) [<1 hashed value>]",
        "THEN 'Dog'"
    })]
    public void TargetingRule_ToString(string configJsonFileName, string settingKey, int targetingRuleIndex, string[] expectedResultLines)
    {
        var pc = ConfigHelper.FromFile(Path.Combine("data", configJsonFileName + ".json"), null, default);
        IConfig config = pc.Config!;
        var setting = config.Settings[settingKey];
        var targetingRule = setting.TargetingRules[targetingRuleIndex];
        var actualResult = targetingRule.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }

    [DataTestMethod]
    [DataRow("test_json_complex", "doubleSetting", new[] { "To all users: '3.14'" })]
    [DataRow("sample_v5", "stringIsNotInDogDefaultCat", new[]
    {
        "IF User.Email IS NOT ONE OF (hashed) [<2 hashed values>]",
        "THEN 'Dog'",
        "To all others: 'Cat'",
    })]
    [DataRow("sample_v5", "string25Cat25Dog25Falcon25Horse", new[]
    {
        "25% of users: 'Cat'",
        "25% of users: 'Dog'",
        "25% of users: 'Falcon'",
        "25% of users: 'Horse'",
        "To unidentified: 'Chicken'",
    })]
    [DataRow("sample_comparators_v6", "countryPercentageAttribute", new[]
    {
        "50% of all Country attributes: 'Falcon'",
        "50% of all Country attributes: 'Horse'",
        "To unidentified: 'Chicken'",
    })]
    [DataRow("sample_v5", "string25Cat25Dog25Falcon25HorseAdvancedRules", new[]
    {
        "IF User.Country IS ONE OF (hashed) [<2 hashed values>]",
        "THEN 'Dolphin'",
        "ELSE IF User.Custom1 CONTAINS ANY OF ['admi']",
        "THEN 'Lion'",
        "ELSE IF User.Email CONTAINS ANY OF ['@configcat.com']",
        "THEN 'Kitten'",
        "OTHERWISE",
        "  25% of users: 'Cat'",
        "  25% of users: 'Dog'",
        "  25% of users: 'Falcon'",
        "  25% of users: 'Horse'",
        "To unidentified: 'Chicken'",
    })]
    [DataRow("sample_comparators_v6", "missingPercentageAttribute", new[]
    {
        "IF User.Email ENDS WITH ANY OF (hashed) [<1 hashed value>]",
        "THEN",
        "  50% of all NotFound attributes: 'Falcon'",
        "  50% of all NotFound attributes: 'Horse'",
        "ELSE IF User.Email ENDS WITH ANY OF (hashed) [<1 hashed value>]",
        "THEN 'NotFound'",
        "To all others: 'Chicken'",
    })]
    [DataRow("sample_and_or_v6", "emailAnd", new[]
    {
        "IF User.Email STARTS WITH ANY OF (hashed) [<1 hashed value>]",
        "  AND User.Email CONTAINS ANY OF ['@']",
        "  AND User.Email ENDS WITH ANY OF (hashed) [<1 hashed value>]",
        "THEN 'Dog'",
        "To all others: 'Cat'",
    })]
    public void Setting_ToString(string configJsonFileName, string settingKey, string[] expectedResultLines)
    {
        var pc = ConfigHelper.FromFile(Path.Combine("data", configJsonFileName + ".json"), null, default);
        IConfig config = pc.Config!;
        var setting = config.Settings[settingKey];
        var actualResult = setting.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }

    [DataTestMethod]
    [DataRow("sample_segments_v6", 0, new[] { "User.Email IS ONE OF (hashed) [<2 hashed values>]" })]
    public void Segment_ToString(string configJsonFileName, int segmentIndex, string[] expectedResultLines)
    {
        var pc = ConfigHelper.FromFile(Path.Combine("data", configJsonFileName + ".json"), null, default);
        IConfig config = pc.Config!;
        var segment = config.Segments[segmentIndex];
        var actualResult = segment.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }
}
