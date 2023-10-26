using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

public class MatrixTestRunner<TDescriptor> : MatrixTestRunnerBase<TDescriptor>
    where TDescriptor : IMatrixTestDescriptor, new()
{
    private static readonly Lazy<MatrixTestRunnerBase<TDescriptor>> DefaultLazy = new(() => new MatrixTestRunner<TDescriptor>(), isThreadSafe: true);
    public static MatrixTestRunnerBase<TDescriptor> Default => DefaultLazy.Value;

    protected override bool AssertValue<T>(string expected, Func<string, T> parse, T actual, string keyName, string? userId)
    {
        Assert.AreEqual(parse(expected), actual, $"config: {DescriptorInstance.ConfigLocation.GetRealLocation()} | keyName: {keyName} | userId: {userId}");
        return true;
    }

    protected override bool AssertVariationId(string expected, string? actual, string keyName, string? userId)
    {
        Assert.AreEqual(expected, actual, $"config: {DescriptorInstance.ConfigLocation.GetRealLocation()} | keyName: {keyName} | userId: {userId}");
        return true;
    }
}
