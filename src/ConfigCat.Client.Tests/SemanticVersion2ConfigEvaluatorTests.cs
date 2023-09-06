using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class SemanticVersion2ConfigEvaluatorTests : ConfigEvaluatorTestsBase<SemanticVersion2ConfigEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d77fa1-a796-85f9-df0c-57c448eb9934/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/q6jMCFIp-EmuAfnmZhPY7w");

        public string MatrixResultFileName => "testmatrix_semantic_2.csv";
    }
}
