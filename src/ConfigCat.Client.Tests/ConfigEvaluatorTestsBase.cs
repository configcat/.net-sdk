using System.Collections.Generic;
using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

public abstract class ConfigEvaluatorTestsBase<TDescriptor> : MatrixTestRunner<TDescriptor>
    where TDescriptor : IMatrixTestDescriptor, new()
{
#pragma warning disable IDE1006 // Naming Styles
    private protected readonly LoggerWrapper Logger;
#pragma warning restore IDE1006 // Naming Styles

    internal readonly IRolloutEvaluator configEvaluator;

    public ConfigEvaluatorTestsBase()
    {
        this.Logger = new ConsoleLogger(LogLevel.Debug).AsWrapper();
        this.configEvaluator = new RolloutEvaluator(this.Logger);
    }

    public static IEnumerable<object?[]> GetMatrixTests() => GetTests();

    [TestCategory("MatrixTests")]
    [DataTestMethod]
    [DynamicData(nameof(GetMatrixTests), DynamicDataSourceType.Method)]
    public void MatrixTests(string jsonFileName, string settingKey, string expectedReturnValue,
        string? userId, string? userEmail, string? userCountry, string? userCustomAttributeName, string? userCustomAttributeValue)
    {
        RunTest(this.configEvaluator, this.Logger, settingKey, expectedReturnValue, userId, userEmail, userCountry, userCustomAttributeName, userCustomAttributeValue);
    }
}
