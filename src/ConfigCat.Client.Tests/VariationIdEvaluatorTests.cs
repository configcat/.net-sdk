using System;
using System.Collections.Generic;
using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class VariationIdEvaluatorTests : ConfigEvaluatorTestsBase
{
    protected override string SampleJsonFileName => "sample_variationid_v5.json";

    protected override string MatrixResultFileName => "testmatrix_variationid.csv";

    protected override void AssertValue(string keyName, string expected, User user)
    {
        var actual = base.configEvaluator.EvaluateVariationId(base.config, keyName, null, user, null, this.Logger).VariationId;

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void EvaluateVariationId_WithSimpleKey_ShouldReturnCat()
    {
        var actual = this.configEvaluator.EvaluateVariationId(base.config, "boolean", string.Empty, user: null, null, this.Logger).VariationId;

        Assert.AreNotEqual(string.Empty, actual);
        Assert.AreEqual("a0e56eda", actual);
    }

    [TestMethod]
    public void EvaluateVariationId_WithNonExistingKey_ShouldReturnDefaultValue()
    {
        var actual = this.configEvaluator.EvaluateVariationId(this.config, "NotExistsKey", "DefaultVariationId", user: null, null, this.Logger).VariationId;

        Assert.AreEqual("DefaultVariationId", actual);
    }

    [TestMethod]
    public void EvaluateVariationId_WithEmptyProjectConfig_ShouldReturnDefaultValue()
    {
        var actual = this.configEvaluator.EvaluateVariationId(new Dictionary<string, Setting>(), "stringDefaultCat", "Default", user: null, null, this.Logger).VariationId;

        Assert.AreEqual("Default", actual);
    }

    [TestMethod]
    public void EvaluateVariationId_WithUser_ShouldReturnEvaluatedValue()
    {
        var actual = this.configEvaluator.EvaluateVariationId(
            this.config,
            "text",
            "defaultVariationId",
            new User("bryanw@verizon.net")
            {
                Email = "bryanw@verizon.net",
                Country = "Hungary"
            },
            null,
            this.Logger).VariationId;

        Assert.AreEqual("30ba32b9", actual);
    }
}
