using System;
using System.Collections.Generic;
using System.Reflection;
using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

public abstract class EvaluationTestsBase
{
    private protected readonly LoggerWrapper logger;
    private protected readonly IRolloutEvaluator configEvaluator;

    public EvaluationTestsBase()
    {
        this.logger = new ConsoleLogger(LogLevel.Debug).AsWrapper();
        this.configEvaluator = new RolloutEvaluator(this.logger);
    }

    private protected abstract Dictionary<string, Setting> BasicConfig { get; }

    [TestMethod]
    public void Evaluate_WithSimpleKey_ShouldReturnCat()
    {
        var actual = this.configEvaluator.Evaluate(BasicConfig, "stringDefaultCat", string.Empty, user: null, null, this.logger).Value;

        Assert.AreNotEqual(string.Empty, actual);
        Assert.AreEqual("Cat", actual);
    }

    [TestMethod]
    public void Evaluate_WithNonExistingKey_ShouldReturnDefaultValue()
    {
        var actual = this.configEvaluator.Evaluate(BasicConfig, "NotExistsKey", "NotExistsValue", user: null, null, this.logger).Value;

        Assert.AreEqual("NotExistsValue", actual);
    }

    [TestMethod]
    public void Evaluate_WithEmptyProjectConfig_ShouldReturnDefaultValue()
    {
        var actual = this.configEvaluator.Evaluate(new Dictionary<string, Setting>(), "stringDefaultCat", "Default", user: null, null, this.logger).Value;

        Assert.AreEqual("Default", actual);
    }

    [TestMethod]
    public void Evaluate_WithUser_ShouldReturnEvaluatedValue()
    {
        var actual = this.configEvaluator.Evaluate(BasicConfig, "doubleDefaultPi", double.NaN, new User("c@configcat.com")
        {
            Email = "c@configcat.com",
            Country = "United Kingdom",
            Custom = { { "Custom1", "admin" } }
        }, null, this.logger).Value;

        Assert.AreEqual(3.1415, actual);
    }

    private delegate EvaluationDetails<object> EvaluateDelegate(IRolloutEvaluator evaluator, Dictionary<string, Setting> settings, string key, object defaultValue, User user,
        ProjectConfig remoteConfig, LoggerWrapper logger);

    private static readonly MethodInfo EvaluateMethodDefinition = new EvaluateDelegate(EvaluationHelper.Evaluate).Method.GetGenericMethodDefinition();

    [DataRow("stringDefaultCat", "", "Cat", typeof(string))]
    [DataRow("stringDefaultCat", "", "Cat", typeof(object))]
    [DataRow("boolDefaultTrue", false, true, typeof(bool))]
    [DataRow("boolDefaultTrue", false, true, typeof(bool?))]
    [DataRow("boolDefaultTrue", false, true, typeof(object))]
    [DataRow("integerDefaultOne", 0, 1, typeof(int))]
    [DataRow("integerDefaultOne", 0, 1, typeof(int?))]
    [DataRow("integerDefaultOne", 0L, 1L, typeof(long))]
    [DataRow("integerDefaultOne", 0L, 1L, typeof(long?))]
    [DataRow("integerDefaultOne", 0, 1, typeof(object))]
    [DataRow("doubleDefaultPi", 0.0, 3.1415, typeof(double))]
    [DataRow("doubleDefaultPi", 0.0, 3.1415, typeof(double?))]
    [DataRow("doubleDefaultPi", 0.0, 3.1415, typeof(object))]
    [DataTestMethod]
    public void Evaluate_WithCompatibleDefaultValue_ShouldSucceed(string key, object defaultValue, object expectedValue, Type settingClrType)
    {
        var args = new object?[]
        {
            this.configEvaluator,
            BasicConfig,
            key,
            defaultValue,
            null,
            null,
            this.logger,
        };

        var evaluationDetails = (EvaluationDetails)EvaluateMethodDefinition.MakeGenericMethod(settingClrType).Invoke(null, args)!;

        Assert.AreEqual(expectedValue, evaluationDetails.Value);
    }

    [DataRow("stringDefaultCat", 0.0, typeof(double?))]
    [DataRow("boolDefaultTrue", "false", typeof(string))]
    [DataRow("integerDefaultOne", "0", typeof(string))]
    [DataRow("doubleDefaultPi", 0, typeof(int))]
    [DataTestMethod]
    public void Evaluate_WithIncompatibleDefaultValueType_ShouldThrowWithImprovedErrorMessage(string key, object defaultValue, Type settingClrType)
    {
        var args = new object?[]
        {
            this.configEvaluator,
            BasicConfig,
            key,
            defaultValue,
            null,
            null,
            this.logger,
        };

        var ex = Assert.ThrowsException<EvaluationErrorException>(() =>
        {
            try { EvaluateMethodDefinition.MakeGenericMethod(settingClrType).Invoke(null, args); }
            catch (TargetInvocationException ex) { throw ex.InnerException!; }
        });
        StringAssert.Contains(ex.Message, $"Setting's type was {BasicConfig[key].SettingType} but the default value's type was {settingClrType}.");
    }
}
