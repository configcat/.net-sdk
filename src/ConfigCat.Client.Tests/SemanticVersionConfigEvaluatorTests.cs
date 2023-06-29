using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class SemanticVersionConfigEvaluatorTests : ConfigEvaluatorTestsBase<SemanticVersionConfigEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        public string SampleJsonFileName => "sample_semantic_v5.json";

        public string MatrixResultFileName => "testmatrix_semantic.csv";
    }
}
