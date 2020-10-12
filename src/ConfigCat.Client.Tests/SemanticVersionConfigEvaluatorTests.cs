using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class SemanticVersionConfigEvaluatorTests : ConfigEvaluatorTestsBase
    {
        protected override string SampleJsonFileName => "sample_semantic_v5.json";

        protected override string MatrixResultFileName => "testmatrix_semantic.csv";
    }
}
