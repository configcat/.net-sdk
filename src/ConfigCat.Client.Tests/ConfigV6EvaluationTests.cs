using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ConfigV6EvaluationTests
{
    public class AndOrMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://test-app.configcat.com/v2/08d89dea-13b2-406b-8ecf-ee94414208a2/08db465d-5756-49ff-8e53-fb90fd760632/08db4c16-c78b-473c-8b68-ca6723c98bfa/08db465d-a64e-4881-8ed0-62b6c9e68e33
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/XUbbCFZX_0mOU_uQ_XYGMg/FfwncdJg1kq0lBqxhYC_7g", "https://test-cdn-eu.configcat.com");
        public string MatrixResultFileName => "testmatrix_and_or.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<AndOrMatrixTestsDescriptor>.GetTests();
    }

    public class ComparatorMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://test-app.configcat.com/v2/08d89dea-13b2-406b-8ecf-ee94414208a2/08db465d-5756-49ff-8e53-fb90fd760632/08db4be6-4a08-4c5c-8c35-30ef3a571a72/08db465d-a64e-4881-8ed0-62b6c9e68e33
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/XUbbCFZX_0mOU_uQ_XYGMg/Lv2mD9Tgx0Km27nuHjw_FA", "https://test-cdn-eu.configcat.com");
        public string MatrixResultFileName => "testmatrix_comparators_v6.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<ComparatorMatrixTestsDescriptor>.GetTests();
    }

    public class FlagDependencyMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://test-app.configcat.com/v2/08d89dea-13b2-406b-8ecf-ee94414208a2/08db465d-5756-49ff-8e53-fb90fd760632/08db4c12-1ff9-47dc-86ca-1186fe1dd43e/08db465d-a64e-4881-8ed0-62b6c9e68e33
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/XUbbCFZX_0mOU_uQ_XYGMg/LGO_8DM9OUGpJixrqqqQcA", "https://test-cdn-eu.configcat.com");
        public string MatrixResultFileName => "testmatrix_prerequisite_flag.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<FlagDependencyMatrixTestsDescriptor>.GetTests();
    }

    public class SegmentMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        // https://test-app.configcat.com/v2/08d89dea-13b2-406b-8ecf-ee94414208a2/08db465d-5756-49ff-8e53-fb90fd760632/08db4c15-8ed0-49d6-8a76-778b50d0bc17/08db465d-a64e-4881-8ed0-62b6c9e68e33
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("configcat-sdk-1/XUbbCFZX_0mOU_uQ_XYGMg/LP0_4hhbQkmVVJcsbO_2Lw", "https://test-cdn-eu.configcat.com");
        public string MatrixResultFileName => "testmatrix_segments.csv";
        public static IEnumerable<object?[]> GetTests() => MatrixTestRunner<SegmentMatrixTestsDescriptor>.GetTests();
    }

    private readonly LoggerWrapper logger;
    private readonly IRolloutEvaluator configEvaluator;

    public ConfigV6EvaluationTests()
    {
        this.logger = new ConsoleLogger(LogLevel.Debug).AsWrapper();
        this.configEvaluator = new RolloutEvaluator(this.logger);
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

    [TestMethod]
    public void CircularDependencyTest()
    {
        var config = new ConfigLocation.LocalFile("data", "sample_circulardependency_v6.json").FetchConfig();

        var logEvents = new List<(LogLevel Level, LogEventId EventId, FormattableLogMessage Message, Exception? Exception)>();

        var loggerMock = new Mock<IConfigCatLogger>();
        loggerMock.SetupGet(logger => logger.LogLevel).Returns(LogLevel.Info);
        loggerMock.Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()))
            .Callback(delegate (LogLevel level, LogEventId eventId, ref FormattableLogMessage msg, Exception ex) { logEvents.Add((level, eventId, msg, ex)); });

        var loggerWrapper = loggerMock.Object.AsWrapper();

        var evaluator = new RolloutEvaluator(loggerWrapper);

        const string key = "key1";
        var evaluationDetails = evaluator.Evaluate<object?>(config!.Settings, key, defaultValue: null, user: null, remoteConfig: null, loggerWrapper);

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

        var inv = loggerMock.Invocations[0];
    }
}
