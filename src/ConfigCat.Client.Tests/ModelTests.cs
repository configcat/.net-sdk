using System;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ModelTests
{
    private const string BasicSampleSdkKey = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";
    private const string AndOrV6SampleSdkKey = "configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/ByMO9yZNn02kXcm72lnY1A";
    private const string ComparatorsV6SampleSdkKey = "configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ";
    private const string FlagDependencyV6SampleSdkKey = "configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/JoGwdqJZQ0K2xDy7LnbyOg";
    private const string SegmentsV6SampleSdkKey = "configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/h99HYXWWNE2bH8eWyLAVMA";

    private static ConfigLocation GetConfigLocation(string? sdkKey, string baseUrlOrFileName)
    {
        return sdkKey is { Length: > 0 }
            ? new ConfigLocation.Cdn(sdkKey, baseUrlOrFileName)
            : new ConfigLocation.LocalFile("data", baseUrlOrFileName + ".json");
    }

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
    [DataRow(BasicSampleSdkKey, null, "stringIsNotInDogDefaultCat", 0, 0, new[] { "User.Email IS NOT ONE OF [<2 hashed values>]" })]
    [DataRow(SegmentsV6SampleSdkKey, null, "countrySegment", 0, 0, new[] { "User IS IN SEGMENT 'United'" })]
    [DataRow(FlagDependencyV6SampleSdkKey, null, "boolDependsOnBool", 0, 0, new[] { "Flag 'mainBoolFlag' EQUALS 'True'" })]
    public void Condition_ToString(string? sdkKey, string baseUrlOrFileName, string settingKey, int targetingRuleIndex, int conditionIndex, string[] expectedResultLines)
    {
        IConfig config = GetConfigLocation(sdkKey, baseUrlOrFileName).FetchConfigCached();
        var setting = config.Settings[settingKey];
        var targetingRule = setting.TargetingRules[targetingRuleIndex];
        var condition = targetingRule.Conditions[conditionIndex];
        var actualResult = condition!.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }

    [DataTestMethod]
    [DataRow(BasicSampleSdkKey, null, "string25Cat25Dog25Falcon25Horse", -1, 0, new[] { "25%: 'Cat'" })]
    [DataRow(ComparatorsV6SampleSdkKey, null, "missingPercentageAttribute", 0, 0, new[] { "50%: 'Falcon'" })]
    public void PercentageOption_ToString(string? sdkKey, string baseUrlOrFileName, string settingKey, int targetingRuleIndex, int percentageOptionIndex, string[] expectedResultLines)
    {
        IConfig config = GetConfigLocation(sdkKey, baseUrlOrFileName).FetchConfigCached();
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
    [DataRow(BasicSampleSdkKey, null, "stringIsNotInDogDefaultCat", 0, new[]
    {
        "IF User.Email IS NOT ONE OF [<2 hashed values>]",
        "THEN 'Dog'",
    })]
    [DataRow(ComparatorsV6SampleSdkKey, null, "missingPercentageAttribute", 0, new[]
    {
        "IF User.Email ENDS WITH ANY OF [<1 hashed value>]",
        "THEN",
        "  50%: 'Falcon'",
        "  50%: 'Horse'",
    })]
    [DataRow(AndOrV6SampleSdkKey, null, "emailAnd", 0, new[]
    {
        "IF User.Email STARTS WITH ANY OF [<1 hashed value>]",
        "  AND User.Email CONTAINS ANY OF ['@']",
        "  AND User.Email ENDS WITH ANY OF [<1 hashed value>]",
        "THEN 'Dog'"
    })]
    public void TargetingRule_ToString(string? sdkKey, string baseUrlOrFileName, string settingKey, int targetingRuleIndex, string[] expectedResultLines)
    {
        IConfig config = GetConfigLocation(sdkKey, baseUrlOrFileName).FetchConfigCached();
        var setting = config.Settings[settingKey];
        var targetingRule = setting.TargetingRules[targetingRuleIndex];
        var actualResult = targetingRule.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }

    [DataTestMethod]
    [DataRow(null, "test_json_complex", "doubleSetting", new[] { "To all users: '3.14'" })]
    [DataRow(BasicSampleSdkKey, null, "stringIsNotInDogDefaultCat", new[]
    {
        "IF User.Email IS NOT ONE OF [<2 hashed values>]",
        "THEN 'Dog'",
        "To all others: 'Cat'",
    })]
    [DataRow(BasicSampleSdkKey, null, "string25Cat25Dog25Falcon25Horse", new[]
    {
        "25% of users: 'Cat'",
        "25% of users: 'Dog'",
        "25% of users: 'Falcon'",
        "25% of users: 'Horse'",
        "To unidentified: 'Chicken'",
    })]
    [DataRow(ComparatorsV6SampleSdkKey, null, "countryPercentageAttribute", new[]
    {
        "50% of all Country attributes: 'Falcon'",
        "50% of all Country attributes: 'Horse'",
        "To unidentified: 'Chicken'",
    })]
    [DataRow(BasicSampleSdkKey, null, "string25Cat25Dog25Falcon25HorseAdvancedRules", new[]
    {
        "IF User.Country IS ONE OF [<2 hashed values>]",
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
    [DataRow(ComparatorsV6SampleSdkKey, null, "missingPercentageAttribute", new[]
    {
        "IF User.Email ENDS WITH ANY OF [<1 hashed value>]",
        "THEN",
        "  50% of all NotFound attributes: 'Falcon'",
        "  50% of all NotFound attributes: 'Horse'",
        "ELSE IF User.Email ENDS WITH ANY OF [<1 hashed value>]",
        "THEN 'NotFound'",
        "To all others: 'Chicken'",
    })]
    [DataRow(AndOrV6SampleSdkKey, null, "emailAnd", new[]
    {
        "IF User.Email STARTS WITH ANY OF [<1 hashed value>]",
        "  AND User.Email CONTAINS ANY OF ['@']",
        "  AND User.Email ENDS WITH ANY OF [<1 hashed value>]",
        "THEN 'Dog'",
        "To all others: 'Cat'",
    })]
    public void Setting_ToString(string? sdkKey, string baseUrlOrFileName, string settingKey, string[] expectedResultLines)
    {
        IConfig config = GetConfigLocation(sdkKey, baseUrlOrFileName).FetchConfigCached();
        var setting = config.Settings[settingKey];
        var actualResult = setting.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }

    [DataTestMethod]
    [DataRow(SegmentsV6SampleSdkKey, null, 0, new[] { "User.Email IS ONE OF [<2 hashed values>]" })]
    public void Segment_ToString(string? sdkKey, string baseUrlOrFileName, int segmentIndex, string[] expectedResultLines)
    {
        IConfig config = GetConfigLocation(sdkKey, baseUrlOrFileName).FetchConfigCached();
        var segment = config.Segments[segmentIndex];
        var actualResult = segment.ToString();
        var expectedResult = string.Join(Environment.NewLine, expectedResultLines);
        Assert.AreEqual(expectedResult, actualResult);
    }
}
