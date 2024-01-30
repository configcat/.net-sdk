using System;
using System.Collections.Generic;
using System.Globalization;
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

    public class VariationIdTestsDescriptor : IMatrixTestDescriptor, IVariationIdMatrixTest
    {
        // https://app.configcat.com/v2/e7a75611-4256-49a5-9320-ce158755e3ba/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08dbc4dc-30c6-4969-8e4c-03f6a8764199/244cf8b0-f604-11e8-b543-f23c917f9d8d
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

    [DataTestMethod]
    [DataRow("key1", "'key1' -> 'key1'")]
    [DataRow("key2", "'key2' -> 'key3' -> 'key2'")]
    [DataRow("key4", "'key4' -> 'key3' -> 'key2' -> 'key3'")]
    public void PrerequisiteFlagCircularDependencyTest(string key, string dependencyCycle)
    {
        var config = new ConfigLocation.LocalFile("data", "test_circulardependency_v6.json").FetchConfig();

        var logger = new Mock<IConfigCatLogger>().Object.AsWrapper();
        var evaluator = new RolloutEvaluator(logger);

        var ex = Assert.ThrowsException<InvalidOperationException>(() => evaluator.Evaluate<object?>(config!.Settings, key, defaultValue: null, user: null, remoteConfig: null, logger));

        StringAssert.Contains(ex.Message, "Circular dependency detected");
        StringAssert.Contains(ex.Message, dependencyCycle);
    }

    [DataTestMethod]
    [DataRow("stringDependsOnBool", "mainBoolFlag", true, "Dog")]
    [DataRow("stringDependsOnBool", "mainBoolFlag", false, "Cat")]
    [DataRow("stringDependsOnBool", "mainBoolFlag", "1", null)]
    [DataRow("stringDependsOnBool", "mainBoolFlag", 1, null)]
    [DataRow("stringDependsOnBool", "mainBoolFlag", 1.0, null)]
    [DataRow("stringDependsOnBool", "mainBoolFlag", new[] { true }, null)]
    [DataRow("stringDependsOnBool", "mainBoolFlag", null, null)]
    [DataRow("stringDependsOnString", "mainStringFlag", "private", "Dog")]
    [DataRow("stringDependsOnString", "mainStringFlag", "Private", "Cat")]
    [DataRow("stringDependsOnString", "mainStringFlag", true, null)]
    [DataRow("stringDependsOnString", "mainStringFlag", 1, null)]
    [DataRow("stringDependsOnString", "mainStringFlag", 1.0, null)]
    [DataRow("stringDependsOnString", "mainStringFlag", new[] { "private" }, null)]
    [DataRow("stringDependsOnString", "mainStringFlag", null, null)]
    [DataRow("stringDependsOnInt", "mainIntFlag", 2, "Dog")]
    [DataRow("stringDependsOnInt", "mainIntFlag", 1, "Cat")]
    [DataRow("stringDependsOnInt", "mainIntFlag", "2", null)]
    [DataRow("stringDependsOnInt", "mainIntFlag", true, null)]
    [DataRow("stringDependsOnInt", "mainIntFlag", 2.0, null)]
    [DataRow("stringDependsOnInt", "mainIntFlag", new[] { 2 }, null)]
    [DataRow("stringDependsOnInt", "mainIntFlag", null, null)]
    [DataRow("stringDependsOnDouble", "mainDoubleFlag", 0.1, "Dog")]
    [DataRow("stringDependsOnDouble", "mainDoubleFlag", 0.11, "Cat")]
    [DataRow("stringDependsOnDouble", "mainDoubleFlag", "0.1", null)]
    [DataRow("stringDependsOnDouble", "mainDoubleFlag", true, null)]
    [DataRow("stringDependsOnDouble", "mainDoubleFlag", 1, null)]
    [DataRow("stringDependsOnDouble", "mainDoubleFlag", new[] { 0.1 }, null)]
    [DataRow("stringDependsOnDouble", "mainDoubleFlag", null, null)]
    public async Task PrerequisiteFlagComparisonValueTypeMismatchTest(string key, string prerequisiteFlagKey, object? prerequisiteFlagValue, object? expectedValue)
    {
        var cdnLocation = (ConfigLocation.Cdn)new FlagDependencyMatrixTestsDescriptor().ConfigLocation;

        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents);

        var overrideDictionary = new Dictionary<string, object> { [prerequisiteFlagKey] = prerequisiteFlagValue! };

        var options = new ConfigCatClientOptions
        {
            FlagOverrides = FlagOverrides.LocalDictionary(overrideDictionary, OverrideBehaviour.LocalOverRemote),
            PollingMode = PollingModes.ManualPoll,
            Logger = logger
        };
        cdnLocation.ConfigureBaseUrl(options);

        using var client = new ConfigCatClient(cdnLocation.SdkKey, options);
        await client.ForceRefreshAsync();

        var actualValue = await client.GetValueAsync(key, (object?)null);
        Assert.AreEqual(expectedValue, actualValue);

        if (expectedValue is null)
        {
            var errors = logEvents.Where(evt => evt.Level == LogLevel.Error).ToArray();
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(1002, errors[0].EventId);
            var ex = errors[0].Exception;
            Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));

            if (prerequisiteFlagValue == null)
            {
                StringAssert.Contains(ex!.Message, "Setting value is null");
            }
            else if (prerequisiteFlagValue.GetType().ToSettingType() == Setting.UnknownType)
            {
                StringAssert.Matches(ex!.Message, new Regex("Setting value '[^']+' is of an unsupported type"));
            }
            else
            {
                StringAssert.Matches(ex!.Message, new Regex("Type mismatch between comparison value '[^']+' and prerequisite flag '[^']+'"));
            }
        }
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

    [TestMethod]
    public void UserObjectAttributeValueConversion_TextComparisons_Test()
    {
        var config = new ConfigLocation.Cdn("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ").FetchConfigCached();

        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents).AsWrapper();
        var evaluator = new RolloutEvaluator(logger);

        const string customAttributeName = "Custom1";
        const int customAttributeValue = 42;
        var user = new User("12345") { Custom = { [customAttributeName] = customAttributeValue } };

        const string key = "boolTextEqualsNumber";
        var evaluationDetails = evaluator.Evaluate<bool?>(config!.Settings, key, defaultValue: null, user, remoteConfig: null, logger);

        Assert.AreEqual(true, evaluationDetails.Value);

        var warnings = logEvents.Where(evt => evt.Level == LogLevel.Warning).ToArray();
        Assert.AreEqual(1, warnings.Length);
        Assert.AreEqual(3005, warnings[0].EventId);

        var message = warnings[0].Message.ToString();
        var expectedAttributeValueText = ((double)customAttributeValue).ToString(CultureInfo.InvariantCulture);
        Assert.AreEqual($"Evaluation of condition (User.{customAttributeName} EQUALS '{expectedAttributeValueText}') for setting '{key}' may not produce the expected result (the User.{customAttributeName} attribute is not a string value, thus it was automatically converted to the string value '{expectedAttributeValueText}'). Please make sure that using a non-string value was intended.", message);
    }

    [DataTestMethod]
    // SemVer-based comparisons
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/iV8vH2MBakKxkFZylxHmTg", "lessThanWithPercentage", "12345", "Custom1", "0.0", "20%")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/iV8vH2MBakKxkFZylxHmTg", "lessThanWithPercentage", "12345", "Custom1", "0.9.9", "< 1.0.0")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/iV8vH2MBakKxkFZylxHmTg", "lessThanWithPercentage", "12345", "Custom1", "1.0.0", "20%")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/iV8vH2MBakKxkFZylxHmTg", "lessThanWithPercentage", "12345", "Custom1", "1.1", "20%")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/iV8vH2MBakKxkFZylxHmTg", "lessThanWithPercentage", "12345", "Custom1", 0, "20%")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/iV8vH2MBakKxkFZylxHmTg", "lessThanWithPercentage", "12345", "Custom1", 0.9, "20%")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/iV8vH2MBakKxkFZylxHmTg", "lessThanWithPercentage", "12345", "Custom1", 2, "20%")]
    // Number-based comparisons
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (sbyte)-1, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (sbyte)2, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (sbyte)3, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (sbyte)5, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (byte)2, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (byte)3, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (byte)5, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (short)-1, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (short)2, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (short)3, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (short)5, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (ushort)2, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (ushort)3, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", (ushort)5, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", -1, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 2, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 3, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 5, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 2u, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 3u, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 5u, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", long.MinValue, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 2L, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 3L, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 5L, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", long.MaxValue, ">5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 2ul, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 3ul, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 5ul, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", ulong.MaxValue, ">5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", float.NegativeInfinity, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", -1f, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 2f, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 2.1f, "<2.1")] // 2.1f < 2.1d as (double)2.1f is 2.0999999046325684 !!! However, this is how IEEE 754 works, so we don't bother about it.
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 3f, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 5f, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", float.PositiveInfinity, ">5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", float.NaN, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", double.NegativeInfinity, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", -1d, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 2d, "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 2.1d, "<=2,1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 3d, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", 5d, ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", double.PositiveInfinity, ">5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", double.NaN, "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "decimal:-79228162514264337593543950335", "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "decimal:2", "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "decimal:2.1", "<=2,1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "decimal:3", "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "decimal:5", ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "decimal:79228162514264337593543950335", ">5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "-Infinity", "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "-1", "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "2", "<2.1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "2.1", "<=2,1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "2,1", "<=2,1")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "3", "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "5", ">=5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "Infinity", ">5")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "NaN", "<>4.2")]
    [DataRow("configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/FCWN-k1dV0iBf8QZrDgjdw", "numberWithPercentage", "12345", "Custom1", "NaNa", "80%")]
    // Date time-based comparisons
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetime:2023-03-31T23:59:59.9990000Z", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetime:2023-04-01T01:59:59.9990000+02:00", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetime:2023-04-01T00:00:00.0010000Z", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetime:2023-04-01T02:00:00.0010000+02:00", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetime:2023-04-30T23:59:59.9990000Z", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetime:2023-05-01T01:59:59.9990000+02:00", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetime:2023-05-01T00:00:00.0010000Z", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetime:2023-05-01T02:00:00.0010000+02:00", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetimeoffset:2023-03-31T23:59:59.9990000Z", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetimeoffset:2023-04-01T01:59:59.9990000+02:00", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetimeoffset:2023-04-01T00:00:00.0010000Z", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetimeoffset:2023-04-01T02:00:00.0010000+02:00", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetimeoffset:2023-04-30T23:59:59.9990000Z", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetimeoffset:2023-05-01T01:59:59.9990000+02:00", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetimeoffset:2023-05-01T00:00:00.0010000Z", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "datetimeoffset:2023-05-01T02:00:00.0010000+02:00", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", double.NegativeInfinity, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", 1680307199.999, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", 1680307200.001, true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", 1682899199.999, true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", 1682899200.001, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", double.PositiveInfinity, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", double.NaN, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", 1680307199, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", 1680307201, true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", 1682899199, true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", 1682899201, false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "-Infinity", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "1680307199.999", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "1680307200.001", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "1682899199.999", true)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "1682899200.001", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "+Infinity", false)]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "boolTrueIn202304", "12345", "Custom1", "NaN", false)]
    // String array-based comparisons
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "stringArrayContainsAnyOfDogDefaultCat", "12345", "Custom1", new string[] { "x", "read" }, "Dog")]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "stringArrayContainsAnyOfDogDefaultCat", "12345", "Custom1", new string[] { "x", "Read" }, "Cat")]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "stringArrayContainsAnyOfDogDefaultCat", "12345", "Custom1", "[\"x\", \"read\"]", "Dog")]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "stringArrayContainsAnyOfDogDefaultCat", "12345", "Custom1", "[\"x\", \"Read\"]", "Cat")]
    [DataRow("configcat-sdk-1/JcPbCGl_1E-K9M-fJOyKyQ/OfQqcTjfFUGBwMKqtyEOrQ", "stringArrayContainsAnyOfDogDefaultCat", "12345", "Custom1", "x, read", "Cat")]
    public void UserObjectAttributeValueConversion_NonTextComparisons_Test(string sdkKey, string key, string? userId, string customAttributeName, object customAttributeValue,
        object expectedReturnValue)
    {
        var config = new ConfigLocation.Cdn(sdkKey).FetchConfigCached();

        var logger = new Mock<IConfigCatLogger>().Object.AsWrapper();
        var evaluator = new RolloutEvaluator(logger);

        if (customAttributeValue is string s)
        {
            const string decimalPrefix = "decimal:", dateTimePrefix = "datetime:", dateTimeOffsetPrefix = "datetimeoffset:";
            if (s.StartsWith(decimalPrefix, StringComparison.Ordinal))
            {
                customAttributeValue = decimal.Parse(s.Substring(decimalPrefix.Length));
            }
            else if (s.StartsWith(dateTimePrefix, StringComparison.Ordinal))
            {
                var dateTimeStyle = s.EndsWith("Z", StringComparison.Ordinal) ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                customAttributeValue = DateTime.ParseExact(s.Substring(dateTimePrefix.Length), "o", CultureInfo.InvariantCulture, dateTimeStyle);
            }
            else if (s.StartsWith(dateTimeOffsetPrefix, StringComparison.Ordinal))
            {
                customAttributeValue = DateTimeOffset.ParseExact(s.Substring(dateTimeOffsetPrefix.Length), "o", CultureInfo.InvariantCulture);
            }
        }

        var user = userId is not null ? new User(userId) { Custom = { [customAttributeName] = customAttributeValue! } } : null;

        var evaluationDetails = evaluator.Evaluate<object?>(config!.Settings, key, defaultValue: null, user, remoteConfig: null, logger);

        Assert.AreEqual(expectedReturnValue, evaluationDetails.Value);
    }

    [DataTestMethod]
    [DataRow("isoneof", "no trim")]
    [DataRow("isnotoneof", "no trim")]
    [DataRow("isoneofhashed", "no trim")]
    [DataRow("isnotoneofhashed", "no trim")]
    [DataRow("equalshashed", "no trim")]
    [DataRow("notequalshashed", "no trim")]
    [DataRow("arraycontainsanyofhashed", "no trim")]
    [DataRow("arraynotcontainsanyofhashed", "no trim")]
    [DataRow("equals", "no trim")]
    [DataRow("notequals", "no trim")]
    [DataRow("startwithanyof", "no trim")]
    [DataRow("notstartwithanyof", "no trim")]
    [DataRow("endswithanyof", "no trim")]
    [DataRow("notendswithanyof", "no trim")]
    [DataRow("arraycontainsanyof", "no trim")]
    [DataRow("arraynotcontainsanyof", "no trim")]
    [DataRow("startwithanyofhashed", "no trim")]
    [DataRow("notstartwithanyofhashed", "no trim")]
    [DataRow("endswithanyofhashed", "no trim")]
    [DataRow("notendswithanyofhashed", "no trim")]
    //semver comparators user values trimmed because of backward compatibility
    [DataRow("semverisoneof", "4 trim")]
    [DataRow("semverisnotoneof", "5 trim")]
    [DataRow("semverless", "6 trim")]
    [DataRow("semverlessequals", "7 trim")]
    [DataRow("semvergreater", "8 trim")]
    [DataRow("semvergreaterequals", "9 trim")]
    //number and date comparators user values trimmed because of backward compatibility
    [DataRow("numberequals", "10 trim")]
    [DataRow("numbernotequals", "11 trim")]
    [DataRow("numberless", "12 trim")]
    [DataRow("numberlessequals", "13 trim")]
    [DataRow("numbergreater", "14 trim")]
    [DataRow("numbergreaterequals", "15 trim")]
    [DataRow("datebefore", "18 trim")]
    [DataRow("dateafter", "19 trim")]
    //"contains any of" and "not contains any of" is a special case, the not trimmed user attribute checked against not trimmed comparator values.
    [DataRow("containsanyof", "no trim")]
    [DataRow("notcontainsanyof", "no trim")]
    public void ComparisonAttributeTrimming_Test(string key, string expectedReturnValue)
    {
        var config = new ConfigLocation.LocalFile("data", "comparison_attribute_trimming.json").FetchConfig();

        var logger = new Mock<IConfigCatLogger>().Object.AsWrapper();
        var evaluator = new RolloutEvaluator(logger);

        var user = new User(" 12345 ")
        {
            Country = "[\" USA \"]",
            Custom =
            {
                ["Version"] = " 1.0.0 ",
                ["Number"] = " 3 ",
                ["Date"] = " 1705253400 "
            }
        };

        const string defaultValue = "default";
        var actualReturnValue = evaluator.Evaluate(config!.Settings, key, defaultValue, user, remoteConfig: null, logger).Value;

        Assert.AreEqual(expectedReturnValue, actualReturnValue);
    }

    [DataTestMethod]
    [DataRow("isoneof", "no trim")]
    [DataRow("isnotoneof", "no trim")]
    [DataRow("containsanyof", "no trim")]
    [DataRow("notcontainsanyof", "no trim")]
    [DataRow("isoneofhashed", "no trim")]
    [DataRow("isnotoneofhashed", "no trim")]
    [DataRow("equalshashed", "no trim")]
    [DataRow("notequalshashed", "no trim")]
    [DataRow("arraycontainsanyofhashed", "no trim")]
    [DataRow("arraynotcontainsanyofhashed", "no trim")]
    [DataRow("equals", "no trim")]
    [DataRow("notequals", "no trim")]
    [DataRow("startwithanyof", "no trim")]
    [DataRow("notstartwithanyof", "no trim")]
    [DataRow("endswithanyof", "no trim")]
    [DataRow("notendswithanyof", "no trim")]
    [DataRow("arraycontainsanyof", "no trim")]
    [DataRow("arraynotcontainsanyof", "no trim")]
    [DataRow("startwithanyofhashed", "default")]
    [DataRow("notstartwithanyofhashed", "default")]
    [DataRow("endswithanyofhashed", "default")]
    [DataRow("notendswithanyofhashed", "default")]
    //semver comparator values trimmed because of backward compatibility
    [DataRow("semverisoneof", "4 trim")]
    [DataRow("semverisnotoneof", "5 trim")]
    [DataRow("semverless", "6 trim")]
    [DataRow("semverlessequals", "7 trim")]
    [DataRow("semvergreater", "8 trim")]
    [DataRow("semvergreaterequals", "9 trim")]
    public void ComparisonValueTrimming_Test(string key, string expectedReturnValue)
    {
        var config = new ConfigLocation.LocalFile("data", "comparison_value_trimming.json").FetchConfig();

        var logger = new Mock<IConfigCatLogger>().Object.AsWrapper();
        var evaluator = new RolloutEvaluator(logger);

        var user = new User("12345")
        {
            Country = "[\"USA\"]",
            Custom =
            {
                ["Version"] = "1.0.0",
                ["Number"] = "3",
                ["Date"] = "1705253400"
            }
        };

        const string defaultValue = "default";
        string actualReturnValue;
        try { actualReturnValue = evaluator.Evaluate(config!.Settings, key, defaultValue, user, remoteConfig: null, logger).Value; }
        catch (InvalidOperationException) { actualReturnValue = defaultValue; }

        Assert.AreEqual(expectedReturnValue, actualReturnValue);
    }
}
