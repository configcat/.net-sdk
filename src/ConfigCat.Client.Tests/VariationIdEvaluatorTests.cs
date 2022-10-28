using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class VariationIdEvaluatorTests : ConfigEvaluatorTestsBase
    {
        protected override string SampleJsonFileName => "sample_variationid_v5.json";

        protected override string MatrixResultFileName => "testmatrix_variationid.csv";

        protected override void AssertValue(string keyName, string expected, User user)
        {
            var actual = base.configEvaluator.EvaluateVariationId(base.config, keyName, null, user);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EvaluateVariationId_WithSimpleKey_ShouldReturnCat()
        {
            string actual = configEvaluator.EvaluateVariationId(base.config, "boolean", string.Empty);

            Assert.AreNotEqual(string.Empty, actual);
            Assert.AreEqual("a0e56eda", actual);
        }

        [TestMethod]
        public void EvaluateVariationId_WithNonExistingKey_ShouldReturnDefaultValue()
        {
            string actual = configEvaluator.EvaluateVariationId(config, "NotExistsKey", "DefaultVariationId");

            Assert.AreEqual("DefaultVariationId", actual);
        }

        [TestMethod]
        public void EvaluateVariationId_WithEmptyProjectConfig_ShouldReturnDefaultValue()
        {
            string actual = configEvaluator.EvaluateVariationId(new Dictionary<string, Setting>(), "stringDefaultCat", "Default");

            Assert.AreEqual("Default", actual);
        }

        [TestMethod]
        public void EvaluateVariationId_WithUser_ShouldReturnEvaluatedValue()
        {
            var actual = configEvaluator.EvaluateVariationId(
                config,
                "text",
                "defaultVariationId",
                new User("bryanw@verizon.net")
                {
                    Email = "bryanw@verizon.net",
                    Country = "Hungary"
                });

            Assert.AreEqual("30ba32b9", actual);
        }
    }
}
