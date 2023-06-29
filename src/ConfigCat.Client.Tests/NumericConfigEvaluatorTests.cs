using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class NumericConfigEvaluatorTests : ConfigEvaluatorTestsBase<NumericConfigEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        public string SampleJsonFileName => "sample_number_v5.json";

        public string MatrixResultFileName => "testmatrix_number.csv";
    }
}
