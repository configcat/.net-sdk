using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ConfigV2EvaluationTests : EvaluationTestsBase
{
    public class BasicTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dbc4dc-1927-4d6b-8fb9-b1472564e2d3/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/AG6C1ngVb0CvM07un6JisQ");
        public string MatrixResultFileName => "testmatrix.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<BasicTestsDescriptor>.GetTests();
    }

    public class NumericTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dbc4dc-0fa3-48d0-8de8-9de55b67fb8b/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw");
        public string MatrixResultFileName => "testmatrix_number.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<NumericTestsDescriptor>.GetTests();
    }

    public class SegmentTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dbd6ca-a85f-4ed0-888a-2da18def92b5/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/y_ZB7o-Xb0Swxth-ZlMSeA");
        public string MatrixResultFileName => "testmatrix_segments_old.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SegmentTestsDescriptor>.GetTests();
    }

    public class SemanticVersionTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dbc4dc-278c-4f83-8d36-db73ad6e2a3a/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/iV8vH2MBakKxkFZylxHmTg");
        public string MatrixResultFileName => "testmatrix_semantic.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SemanticVersionTestsDescriptor>.GetTests();
    }

    public class SemanticVersion2TestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dbc4dc-2b2b-451e-8359-abdef494c2a2/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/U8nt3zEhDEO5S2ulubCopA");
        public string MatrixResultFileName => "testmatrix_semantic_2.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SemanticVersion2TestsDescriptor>.GetTests();
    }

    public class SensitiveTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dbc4dc-2d62-4e1b-884b-6aa237b34764/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/-0YmVOUNgEGKkgRF-rU65g");
        public string MatrixResultFileName => "testmatrix_sensitive.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SensitiveTestsDescriptor>.GetTests();
    }

    public class VariationIdTestsDescriptor : IMatrixTestDescriptor, IVariationIdMatrixText
    {
        //https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dbc4dc-30c6-4969-8e4c-03f6a8764199/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/spQnkRTIPEWVivZkWM84lQ");
        public string MatrixResultFileName => "testmatrix_variationid.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<VariationIdTestsDescriptor>.GetTests();
    }

    public class AndOrMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08dbc325-7f69-4fd4-8af4-cf9f24ec8ac9/08dbc325-9d5e-4988-891c-fd4a45790bd1/08dbc325-9ebd-4587-8171-88f76a3004cb
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/ByMO9yZNn02kXcm72lnY1A");
        public string MatrixResultFileName => "testmatrix_and_or.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<AndOrMatrixTestsDescriptor>.GetTests();
    }

    public class ComparatorMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08dbc325-7f69-4fd4-8af4-cf9f24ec8ac9/08dbc325-9a6b-4947-84e2-91529248278a/08dbc325-9ebd-4587-8171-88f76a3004cb
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ");
        public string MatrixResultFileName => "testmatrix_comparators_v6.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<ComparatorMatrixTestsDescriptor>.GetTests();
    }

    public class FlagDependencyMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08dbc325-7f69-4fd4-8af4-cf9f24ec8ac9/08dbc325-9b74-45cb-86d0-4d61c25af1aa/08dbc325-9ebd-4587-8171-88f76a3004cb
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/JoGwdqJZQ0K2xDy7LnbyOg");
        public string MatrixResultFileName => "testmatrix_prerequisite_flag.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<FlagDependencyMatrixTestsDescriptor>.GetTests();
    }

    public class SegmentMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08dbc325-7f69-4fd4-8af4-cf9f24ec8ac9/08dbc325-9cfb-486f-8906-72a57c693615/08dbc325-9ebd-4587-8171-88f76a3004cb
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/h99HYXWWNE2bH8eWyLAVMA");
        public string MatrixResultFileName => "testmatrix_segments.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SegmentMatrixTestsDescriptor>.GetTests();
    }

    public class UnicodeMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08dbc325-7f69-4fd4-8af4-cf9f24ec8ac9/08dbd63c-9774-49d6-8187-5f2aab7bd606/08dbc325-9ebd-4587-8171-88f76a3004cb
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/Da6w8dBbmUeMUBhh0iEeQQ");
        public string MatrixResultFileName => "testmatrix_unicode.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<UnicodeMatrixTestsDescriptor>.GetTests();
    }

    private protected override Dictionary<string, Setting> BasicConfig => MatrixTestRunner<BasicTestsDescriptor>.Default.config;

    [DataTestMethod]
    [DynamicData(nameof(BasicTestsDescriptor.GetTests), typeof(BasicTestsDescriptor), DynamicDataSourceType.Method)]
    public void BasicTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<BasicTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(NumericTestsDescriptor.GetTests), typeof(NumericTestsDescriptor), DynamicDataSourceType.Method)]
    public void NumericTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<NumericTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(SegmentTestsDescriptor.GetTests), typeof(SegmentTestsDescriptor), DynamicDataSourceType.Method)]
    public void SegmentTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<SegmentTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(SemanticVersionTestsDescriptor.GetTests), typeof(SemanticVersionTestsDescriptor), DynamicDataSourceType.Method)]
    public void SemanticVersionTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<SemanticVersionTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(SemanticVersion2TestsDescriptor.GetTests), typeof(SemanticVersion2TestsDescriptor), DynamicDataSourceType.Method)]
    public void SemanticVersion2Tests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<SemanticVersion2TestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(SensitiveTestsDescriptor.GetTests), typeof(SensitiveTestsDescriptor), DynamicDataSourceType.Method)]
    public void SensitiveTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<SensitiveTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(VariationIdTestsDescriptor.GetTests), typeof(VariationIdTestsDescriptor), DynamicDataSourceType.Method)]
    public void VariationIdTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<VariationIdTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(AndOrMatrixTestsDescriptor.GetTests), typeof(AndOrMatrixTestsDescriptor), DynamicDataSourceType.Method)]
    public void AndOrMatrixTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<AndOrMatrixTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(ComparatorMatrixTestsDescriptor.GetTests), typeof(ComparatorMatrixTestsDescriptor), DynamicDataSourceType.Method)]
    public void ComparatorMatrixTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<ComparatorMatrixTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(FlagDependencyMatrixTestsDescriptor.GetTests), typeof(FlagDependencyMatrixTestsDescriptor), DynamicDataSourceType.Method)]
    public void FlagDependencyMatrixTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<FlagDependencyMatrixTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [DataTestMethod]
    [DynamicData(nameof(SegmentMatrixTestsDescriptor.GetTests), typeof(SegmentMatrixTestsDescriptor), DynamicDataSourceType.Method)]
    public void SegmentMatrixTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<SegmentMatrixTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }


    [DataTestMethod]
    [DynamicData(nameof(UnicodeMatrixTestsDescriptor.GetTests), typeof(UnicodeMatrixTestsDescriptor), DynamicDataSourceType.Method)]
    public void UnicodeMatrixTests(string configLocation, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        MatrixTestRunner<UnicodeMatrixTestsDescriptor>.Default.RunTest(this.configEvaluator, this.logger, settingKey, expectedReturnValue,
            userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }

    [TestMethod]
    public void CircularDependencyTest()
    {
        var config = new ConfigLocation.LocalFile("data", "test_circulardependency_v6.json").FetchConfig();

        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents);

        var evaluator = new RolloutEvaluator(logger);

        const string key = "key1";
        var evaluationDetails = evaluator.Evaluate<object?>(config!.Settings, key, defaultValue: null, user: null, remoteConfig: null, logger);

        Assert.AreEqual(4, logEvents.Count);

        Assert.AreEqual(3, logEvents.Count(evt => evt.EventId == 3005));

        Assert.IsTrue(logEvents.Any(evt => evt.Level == LogLevel.Warning
            && (string?)evt.Message.ArgValues[1] == "key1"
            && (string?)evt.Message.ArgValues[2] == "'key1' -> 'key1'"));

        Assert.IsTrue(logEvents.Any(evt => evt.Level == LogLevel.Warning
            && (string?)evt.Message.ArgValues[1] == "key2"
            && (string?)evt.Message.ArgValues[2] == "'key1' -> 'key2' -> 'key1'"));

        Assert.IsTrue(logEvents.Any(evt => evt.Level == LogLevel.Warning
            && (string?)evt.Message.ArgValues[1] == "key3"
            && (string?)evt.Message.ArgValues[2] == "'key1' -> 'key3' -> 'key3'"));

        var evaluateLogEvent = logEvents.FirstOrDefault(evt => evt.Level == LogLevel.Info && evt.EventId == 5000);
        Assert.IsNotNull(evaluateLogEvent);

        StringAssert.Matches((string?)evaluateLogEvent.Message.ArgValues[0], new Regex(
            "THEN 'key1-prereq1' => " + Regex.Escape(RolloutEvaluator.CircularDependencyError) + Environment.NewLine
            + @"\s+" + Regex.Escape(RolloutEvaluator.TargetingRuleIgnoredMessage)));

        StringAssert.Matches((string?)evaluateLogEvent.Message.ArgValues[0], new Regex(
            "THEN 'key2-prereq1' => " + Regex.Escape(RolloutEvaluator.CircularDependencyError) + Environment.NewLine
            + @"\s+" + Regex.Escape(RolloutEvaluator.TargetingRuleIgnoredMessage)));

        StringAssert.Matches((string?)evaluateLogEvent.Message.ArgValues[0], new Regex(
            "THEN 'key3-prereq1' => " + Regex.Escape(RolloutEvaluator.CircularDependencyError) + Environment.NewLine
            + @"\s+" + Regex.Escape(RolloutEvaluator.TargetingRuleIgnoredMessage)));
    }

    [DataTestMethod]
    [DataRow("stringDependsOnString", "1", "john@sensitivecompany.com", null, "Dog")]
    [DataRow("stringDependsOnString", "1", "john@sensitivecompany.com", OverrideBehaviour.RemoteOverLocal, "Dog")]
    [DataRow("stringDependsOnString", "1", "john@sensitivecompany.com", OverrideBehaviour.LocalOverRemote, "Dog")]
    [DataRow("stringDependsOnString", "1", "john@sensitivecompany.com", OverrideBehaviour.LocalOnly, null)]
    [DataRow("stringDependsOnString", "2", "john@notsensitivecompany.com", null, "Cat")]
    [DataRow("stringDependsOnString", "2", "john@notsensitivecompany.com", OverrideBehaviour.RemoteOverLocal, "Cat")]
    [DataRow("stringDependsOnString", "2", "john@notsensitivecompany.com", OverrideBehaviour.LocalOverRemote, "Dog")]
    [DataRow("stringDependsOnString", "2", "john@notsensitivecompany.com", OverrideBehaviour.LocalOnly, null)]
    [DataRow("stringDependsOnInt", "1", "john@sensitivecompany.com", null, "Dog")]
    [DataRow("stringDependsOnInt", "1", "john@sensitivecompany.com", OverrideBehaviour.RemoteOverLocal, "Dog")]
    [DataRow("stringDependsOnInt", "1", "john@sensitivecompany.com", OverrideBehaviour.LocalOverRemote, "Cat")]
    [DataRow("stringDependsOnInt", "1", "john@sensitivecompany.com", OverrideBehaviour.LocalOnly, null)]
    [DataRow("stringDependsOnInt", "2", "john@notsensitivecompany.com", null, "Cat")]
    [DataRow("stringDependsOnInt", "2", "john@notsensitivecompany.com", OverrideBehaviour.RemoteOverLocal, "Cat")]
    [DataRow("stringDependsOnInt", "2", "john@notsensitivecompany.com", OverrideBehaviour.LocalOverRemote, "Dog")]
    [DataRow("stringDependsOnInt", "2", "john@notsensitivecompany.com", OverrideBehaviour.LocalOnly, null)]
    public async Task PrerequisiteFlagOverrideTest(string key, string userId, string email, OverrideBehaviour? overrideBehaviour, object expectedValue)
    {
        var cdnLocation = (ConfigLocation.Cdn)new FlagDependencyMatrixTestsDescriptor().ConfigLocation;

        var options = new ConfigCatClientOptions
        {
            // The flag override alters the definition of the following flags:
            // * 'mainStringFlag': to check the case where a prerequisite flag is overridden (dependent flag: 'stringDependsOnString')
            // * 'stringDependsOnInt': to check the case where a dependent flag is overridden (prerequisite flag: 'mainIntFlag')
            FlagOverrides = overrideBehaviour is not null
                ? FlagOverrides.LocalFile(Path.Combine("data", "test_override_flagdependency_v6.json"), autoReload: false, overrideBehaviour.Value)
                : null,
            PollingMode = PollingModes.ManualPoll,
        };
        cdnLocation.ConfigureBaseUrl(options);

        using var client = new ConfigCatClient(cdnLocation.SdkKey, options);
        await client.ForceRefreshAsync();
        var actualValue = await client.GetValueAsync(key, (object?)null, new User(userId) { Email = email });

        Assert.AreEqual(expectedValue, actualValue);
    }

    [DataTestMethod]
    [DataRow("developerAndBetaUserSegment", "1", "john@example.com", null, false)]
    [DataRow("developerAndBetaUserSegment", "1", "john@example.com", OverrideBehaviour.RemoteOverLocal, false)]
    [DataRow("developerAndBetaUserSegment", "1", "john@example.com", OverrideBehaviour.LocalOverRemote, true)]
    [DataRow("developerAndBetaUserSegment", "1", "john@example.com", OverrideBehaviour.LocalOnly, true)]
    [DataRow("notDeveloperAndNotBetaUserSegment", "2", "kate@example.com", null, true)]
    [DataRow("notDeveloperAndNotBetaUserSegment", "2", "kate@example.com", OverrideBehaviour.RemoteOverLocal, true)]
    [DataRow("notDeveloperAndNotBetaUserSegment", "2", "kate@example.com", OverrideBehaviour.LocalOverRemote, true)]
    [DataRow("notDeveloperAndNotBetaUserSegment", "2", "kate@example.com", OverrideBehaviour.LocalOnly, null)]
    public async Task ConfigSaltAndSegmentsOverrideTest(string key, string userId, string email, OverrideBehaviour? overrideBehaviour, object expectedValue)
    {
        var cdnLocation = (ConfigLocation.Cdn)new SegmentMatrixTestsDescriptor().ConfigLocation;

        var options = new ConfigCatClientOptions
        {
            // The flag override uses a different config json salt than the downloaded one and overrides the following segments:
            // * 'Beta Users': User.Email IS ONE OF ['jane@example.com']
            // * 'Developers': User.Email IS ONE OF ['john@example.com']
            FlagOverrides = overrideBehaviour is not null
                ? FlagOverrides.LocalFile(Path.Combine("data", "test_override_segments_v6.json"), autoReload: false, overrideBehaviour.Value)
                : null,
            PollingMode = PollingModes.ManualPoll,
        };
        cdnLocation.ConfigureBaseUrl(options);

        using var client = new ConfigCatClient(cdnLocation.SdkKey, options);
        await client.ForceRefreshAsync();
        var actualValue = await client.GetValueAsync(key, (object?)null, new User(userId) { Email = email });

        Assert.AreEqual(expectedValue, actualValue);
    }

    // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08dbc325-7f69-4fd4-8af4-cf9f24ec8ac9/08dbc325-9e4e-4f59-86b2-5da50924b6ca/08dbc325-9ebd-4587-8171-88f76a3004cb
    [DataTestMethod]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", null, null, null, "Cat", false, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", "12345", null, null, "Cat", false, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", "12345", "a@example.com", null, "Dog", true, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", "12345", "a@configcat.com", null, "Cat", false, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", "12345", "a@configcat.com", "", "Frog", true, true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", "12345", "a@configcat.com", "US", "Fish", true, true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", "12345", "b@configcat.com", null, "Cat", false, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", "12345", "b@configcat.com", "", "Falcon", false, true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/P4e3fAz_1ky2-Zg2e4cbkw", "stringMatchedTargetingRuleAndOrPercentageOption", "12345", "b@configcat.com", "US", "Spider", false, true)]
    public void EvaluationDetails_MatchedEvaluationRuleAndPercantageOption_Test(string sdkKey, string key, string? userId, string? email, string? percentageBase,
        string expectedReturnValue, bool expectedIsExpectedMatchedTargetingRuleSet, bool expectedIsExpectedMatchedPercentageOptionSet)
    {
        var config = new ConfigLocation.Cdn(sdkKey).FetchConfigCached();

        var logger = new Mock<IConfigCatLogger>().Object.AsWrapper();
        var evaluator = new RolloutEvaluator(logger);

        var user = userId is not null ? new User(userId) { Email = email, Custom = { ["PercentageBase"] = percentageBase! } } : null;

        var evaluationDetails = evaluator.Evaluate<object?>(config!.Settings, key, defaultValue: null, user, remoteConfig: null, logger);

        Assert.AreEqual(expectedReturnValue, evaluationDetails.Value);
        Assert.AreEqual(expectedIsExpectedMatchedTargetingRuleSet, evaluationDetails.MatchedTargetingRule is not null);
        Assert.AreEqual(expectedIsExpectedMatchedPercentageOptionSet, evaluationDetails.MatchedPercentageOption is not null);
    }
}
