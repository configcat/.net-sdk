using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class NumericConfigEvaluatorTests : ConfigEvaluatorTestsBase
    {
        protected override string SampleJsonFileName => "sample_number_v3.json";

        protected override string MatrixResultFileName => "testmatrix_number.csv";
    }
}
