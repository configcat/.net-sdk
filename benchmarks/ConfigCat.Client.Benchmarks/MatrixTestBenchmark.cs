using System;
using BenchmarkDotNet.Attributes;

namespace ConfigCat.Client.Benchmarks;

[MemoryDiagnoser]
public class MatrixTestBenchmark
{
    private Old.MatrixTestRunnerBase<Old.BenchmarkHelper.BasicMatrixTestsDescriptor> testRunnerOld = null!;
    private object evaluationServicesOld = null!;

    private New.MatrixTestRunnerBase<New.BenchmarkHelper.BasicMatrixTestsDescriptor> testRunnerNew = null!;
    private object evaluationServicesNew = null!;

    private object?[][] tests = null!;

    [GlobalSetup]
    public void Setup()
    {
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        this.testRunnerOld = new();
        this.evaluationServicesOld = Old.BenchmarkHelper.CreateEvaluationServices(LogInfo);

        this.testRunnerNew = new();
        this.evaluationServicesNew = New.BenchmarkHelper.CreateEvaluationServices(LogInfo);

        this.tests = New.BenchmarkHelper.GetMatrixTests<New.BenchmarkHelper.BasicMatrixTestsDescriptor>();
    }

    [Params(false, true)]
    public bool LogInfo { get; set; }

    [Benchmark]
    public int MatrixTests_v9()
    {
        return Old.BenchmarkHelper.RunAllMatrixTests(this.testRunnerOld, this.evaluationServicesOld, this.tests);
    }

    [Benchmark]
    public int MatrixTests_vNext()
    {
        return New.BenchmarkHelper.RunAllMatrixTests(this.testRunnerNew, this.evaluationServicesNew, this.tests);
    }
}
