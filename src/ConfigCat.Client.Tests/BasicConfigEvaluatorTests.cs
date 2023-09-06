using System;
using System.Collections.Generic;
using System.Reflection;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class BasicConfigEvaluatorTests : ConfigEvaluatorTestsBase<BasicConfigEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d62463-86ec-8fde-f5b5-1c5c426fc830/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A");

        public string MatrixResultFileName => "testmatrix.csv";
    }

    [TestMethod]
    public void GetValue_WithSimpleKey_ShouldReturnCat()
    {
        var actual = this.configEvaluator.Evaluate(this.config, "stringDefaultCat", string.Empty, user: null, null, this.Logger).Value;

        Assert.AreNotEqual(string.Empty, actual);
        Assert.AreEqual("Cat", actual);
    }

    [TestMethod]
    public void GetValue_WithNonExistingKey_ShouldReturnDefaultValue()
    {
        var actual = this.configEvaluator.Evaluate(this.config, "NotExistsKey", "NotExistsValue", user: null, null, this.Logger).Value;

        Assert.AreEqual("NotExistsValue", actual);
    }

    [TestMethod]
    public void GetValue_WithEmptyProjectConfig_ShouldReturnDefaultValue()
    {
        var actual = this.configEvaluator.Evaluate(new Dictionary<string, Setting>(), "stringDefaultCat", "Default", user: null, null, this.Logger).Value;

        Assert.AreEqual("Default", actual);
    }

    [TestMethod]
    public void GetValue_WithUser_ShouldReturnEvaluatedValue()
    {
        var actual = this.configEvaluator.Evaluate(this.config, "doubleDefaultPi", double.NaN, new User("c@configcat.com")
        {
            Email = "c@configcat.com",
            Country = "United Kingdom",
            Custom = { { "Custom1", "admin" } }
        }, null, this.Logger).Value;

        Assert.AreEqual(3.1415, actual);
    }

    private delegate EvaluationDetails<object> EvaluateDelegate(IRolloutEvaluator evaluator, Dictionary<string, Setting> settings, string key, object defaultValue, User user,
        ProjectConfig remoteConfig, LoggerWrapper logger);

    private static readonly MethodInfo EvaluateMethodDefinition = new EvaluateDelegate(RolloutEvaluatorExtensions.Evaluate).Method.GetGenericMethodDefinition();

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
    public void GetValue_WithCompatibleDefaultValue_ShouldSucceed(string key, object defaultValue, object expectedValue, Type settingClrType)
    {
        var args = new object?[]
        {
            this.configEvaluator,
            this.config,
            key,
            defaultValue,
            null,
            null,
            this.Logger,
        };

        var evaluationDetails = (EvaluationDetails)EvaluateMethodDefinition.MakeGenericMethod(settingClrType).Invoke(null, args)!;

        Assert.AreEqual(expectedValue, evaluationDetails.Value);
    }

    [DataRow("stringDefaultCat", 0.0, typeof(double?))]
    [DataRow("boolDefaultTrue", "false", typeof(string))]
    [DataRow("integerDefaultOne", "0", typeof(string))]
    [DataRow("doubleDefaultPi", 0, typeof(int))]
    [DataTestMethod]
    public void GetValue_WithIncompatibleDefaultValueType_ShouldThrowWithImprovedErrorMessage(string key, object defaultValue, Type settingClrType)
    {
        var args = new object?[]
        {
            this.configEvaluator,
            this.config,
            key,
            defaultValue,
            null,
            null,
            this.Logger,
        };

        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
        {
            try { EvaluateMethodDefinition.MakeGenericMethod(settingClrType).Invoke(null, args); }
            catch (TargetInvocationException ex) { throw ex.InnerException!; }
        });
        StringAssert.Contains(ex.Message, $"Setting's type was {this.config[key].SettingType} but the default value's type was {settingClrType}.");
    }
}
