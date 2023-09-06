using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class SemanticVersionConfigEvaluatorTests : ConfigEvaluatorTestsBase<SemanticVersionConfigEvaluatorTests.Descriptor>
{
    public class Descriptor : IMatrixTestDescriptor
    {
        // https://app.configcat.com/08d5a03c-feb7-af1e-a1fa-40b3329f8bed/08d745f1-f315-7daf-d163-5541d3786e6f/244cf8b0-f604-11e8-b543-f23c917f9d8d
        public ConfigLocation ConfigLocation => new ConfigLocation.Cdn("PKDVCLf-Hq-h-kCzMp-L7Q/BAr3KgLTP0ObzKnBTo5nhA");

        public string MatrixResultFileName => "testmatrix_semantic.csv";
    }
}
