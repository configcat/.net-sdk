using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class SemanticVersion2ConfigEvaluatorTests : ConfigEvaluatorTestsBase<SemanticVersion2ConfigEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        public string SampleJsonFileName => "sample_semantic_2_v5.json";

        public string MatrixResultFileName => "testmatrix_semantic_2.csv";
    }
}
