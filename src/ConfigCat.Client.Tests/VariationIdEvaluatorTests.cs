using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class VariationIdEvaluatorTests : ConfigEvaluatorTestsBase
    {
        protected override string SampleJsonFileName => "sample_variationid_v4.json";

        protected override string MatrixResultFileName => "testmatrix_variationid.csv";

        protected override void AssertValue(string keyName, string expected, User user)
        {
            var actual = base.configEvaluator.EvaluateVariationId(base.config, keyName, null, user);

            Assert.AreEqual(expected, actual);
        }
    }
}
