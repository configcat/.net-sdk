using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class NumericConfigEvaluatorTests : ConfigEvaluatorTestsBase<NumericConfigEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d747f0-5986-c2ef-eef3-ec778e32e10a/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/uGyK3q9_ckmdxRyI7vjwCw");

        public string MatrixResultFileName => "testmatrix_number.csv";
    }
}
