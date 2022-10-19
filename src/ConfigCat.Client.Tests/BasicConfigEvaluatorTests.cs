using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class BasicConfigEvaluatorTests : ConfigEvaluatorTestsBase
    {
        protected override string SampleJsonFileName => "sample_v5.json";

        protected override string MatrixResultFileName => "testmatrix.csv";

        [TestMethod]
        public void GetValue_WithSimpleKey_ShouldReturnCat()
        {
            string actual = configEvaluator.Evaluate(config, "stringDefaultCat", string.Empty, user: null, null, this.logger, out _);

            Assert.AreNotEqual(string.Empty, actual);
            Assert.AreEqual("Cat", actual);
        }

        [TestMethod]
        public void GetValue_WithNonExistingKey_ShouldReturnDefaultValue()
        {
            string actual = configEvaluator.Evaluate(config, "NotExistsKey", "NotExistsValue", user: null, null, this.logger, out _);

            Assert.AreEqual("NotExistsValue", actual);
        }

        [TestMethod]
        public void GetValue_WithEmptyProjectConfig_ShouldReturnDefaultValue()
        {
            string actual = configEvaluator.Evaluate(new Dictionary<string, Setting>(), "stringDefaultCat", "Default", user: null, null, this.logger, out _);

            Assert.AreEqual("Default", actual);
        }

        [TestMethod]
        public void GetValue_WithUser_ShouldReturnEvaluatedValue()
        {
            double actual = configEvaluator.Evaluate(config, "doubleDefaultPi", double.NaN, new User("c@configcat.com")
            {
                Email = "c@configcat.com",
                Country = "United Kingdom",
                Custom = new Dictionary<string, string> { { "Custom1", "admin" } }
            }, null, this.logger, out _);

            Assert.AreEqual(3.1415, actual);
        }
    }
}
