using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class SensitiveEvaluatorTests : ConfigEvaluatorTestsBase<SensitiveEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d7b724-9285-f4a7-9fcd-00f64f1e83d5/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/qX3TP2dTj06ZpCCT1h_SPA");

        public string MatrixResultFileName => "testmatrix_sensitive.csv";
    }
}
