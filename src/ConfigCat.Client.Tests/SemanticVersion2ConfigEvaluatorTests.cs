using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class SemanticVersion2ConfigEvaluatorTests : ConfigEvaluatorTestsBase
    {
        protected override string SampleJsonFileName => "sample_semantic_2_v3.json";

        protected override string MatrixResultFileName => "testmatrix_semantic_2.csv";
    }
}
