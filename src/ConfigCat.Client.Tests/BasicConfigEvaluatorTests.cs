using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class BasicConfigEvaluatorTests : ConfigEvaluatorTestsBase
{
    protected override string SampleJsonFileName => "sample_v5.json";

    protected override string MatrixResultFileName => "testmatrix.csv";

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
            Custom = new Dictionary<string, string> { { "Custom1", "admin" } }
        }, null, this.Logger).Value;

        Assert.AreEqual(3.1415, actual);
    }

    private delegate EvaluationDetails<object> EvaluateDelegate(IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, object defaultValue, User user,
        ProjectConfig remoteConfig, ILogger logger);

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
        var args = new object[]
        {
            this.configEvaluator,
            this.config,
            key,
            defaultValue,
            null,
            null,
            this.Logger,
        };

        var evaluationDetails = (EvaluationDetails)EvaluateMethodDefinition.MakeGenericMethod(settingClrType).Invoke(null, args);

        Assert.AreEqual(expectedValue, evaluationDetails.Value);
    }

    [DataRow("stringDefaultCat", 0.0, typeof(double?))]
    [DataRow("boolDefaultTrue", "false", typeof(string))]
    [DataRow("integerDefaultOne", "0", typeof(string))]
    [DataRow("doubleDefaultPi", 0, typeof(int))]
    [DataTestMethod]
    public void GetValue_WithIncompatibleDefaultValueType_ShouldThrowWithImprovedErrorMessage(string key, object defaultValue, Type settingClrType)
    {
        var args = new object[]
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
            catch (TargetInvocationException ex) { throw ex.InnerException; }
        });
        StringAssert.Contains(ex.Message, $"Setting's type was {this.config[key].SettingType} but the default value's type was {settingClrType}.");
    }
}
