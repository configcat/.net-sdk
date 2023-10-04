using System.Collections.Generic;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ConfigV5EvaluationTests : EvaluationTestsBase
{
    public class BasicTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d62463-86ec-8fde-f5b5-1c5c426fc830/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A");
        public string MatrixResultFileName => "testmatrix.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<BasicTestsDescriptor>.GetTests();
    }

    public class NumericTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d747f0-5986-c2ef-eef3-ec778e32e10a/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/uGyK3q9_ckmdxRyI7vjwCw");
        public string MatrixResultFileName => "testmatrix_number.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<NumericTestsDescriptor>.GetTests();
    }

    public class SemanticVersionTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d745f1-f315-7daf-d163-5541d3786e6f/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/BAr3KgLTP0ObzKnBTo5nhA");
        public string MatrixResultFileName => "testmatrix_semantic.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SemanticVersionTestsDescriptor>.GetTests();
    }

    public class SemanticVersion2TestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d77fa1-a796-85f9-df0c-57c448eb9934/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/q6jMCFIp-EmuAfnmZhPY7w");
        public string MatrixResultFileName => "testmatrix_semantic_2.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SemanticVersion2TestsDescriptor>.GetTests();
    }

    public class SensitiveTestsDescriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d7b724-9285-f4a7-9fcd-00f64f1e83d5/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/qX3TP2dTj06ZpCCT1h_SPA");
        public string MatrixResultFileName => "testmatrix_sensitive.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SensitiveTestsDescriptor>.GetTests();
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
}
