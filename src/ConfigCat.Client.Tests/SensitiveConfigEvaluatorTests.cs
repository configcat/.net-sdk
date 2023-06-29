using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class SensitiveEvaluatorTests : ConfigEvaluatorTestsBase<SensitiveEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        public string SampleJsonFileName => "sample_sensitive_v5.json";

        public string MatrixResultFileName => "testmatrix_sensitive.csv";
    }
}
