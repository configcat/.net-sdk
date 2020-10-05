using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class SensitiveEvaluatorTests : ConfigEvaluatorTestsBase
    {
        protected override string SampleJsonFileName => "sample_sensitive_v5.json";

        protected override string MatrixResultFileName => "testmatrix_sensitive.csv";
    }
}
