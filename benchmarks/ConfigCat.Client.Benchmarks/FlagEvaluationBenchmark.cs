extern alias from_nuget;
extern alias from_project;

using System;
using BenchmarkDotNet.Attributes;

namespace ConfigCat.Client.Benchmarks;

[MemoryDiagnoser]
public class FlagEvaluationBenchmark
{
    private object evaluationServicesOld = null!;
    private from_nuget::ConfigCat.Client.User userOld = null!;

    private object evaluationServicesNew = null!;
    private from_project::ConfigCat.Client.User userNew = null!;

    [GlobalSetup]
    public void Setup()
    {
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        this.evaluationServicesOld = Old.BenchmarkHelper.CreateEvaluationServices(LogInfo);
        this.userOld = new("Cat") { Email = "cat@configcat.com", Custom = { ["Version"] = "1.1.1", ["Number"] = "1" } };

        this.evaluationServicesNew = New.BenchmarkHelper.CreateEvaluationServices(LogInfo);
        this.userNew = new("Cat") { Email = "cat@configcat.com", Custom = { ["Version"] = "1.1.1", ["Number"] = "1" } };
    }

    [Params(false, true)]
    public bool LogInfo { get; set; }

    [Benchmark]
    public object Basic_ConfigV5()
    {
        return Old.BenchmarkHelper.Evaluate(this.evaluationServicesOld, "basicFlag", false);
    }

    [Benchmark]
    public object Basic_ConfigV6()
    {
        return New.BenchmarkHelper.Evaluate(this.evaluationServicesNew, "basicFlag", false);
    }

    [Benchmark]
    public object Complex_ConfigV5()
    {
        return Old.BenchmarkHelper.Evaluate(this.evaluationServicesOld, "complexFlag", "", this.userOld);
    }

    [Benchmark]
    public object Complex_ConfigV6()
    {
        return New.BenchmarkHelper.Evaluate(this.evaluationServicesNew, "complexFlag", "", this.userNew);
    }

    [Benchmark]
    public object All_ConfigV5()
    {
        return Old.BenchmarkHelper.EvaluateAll(this.evaluationServicesOld, this.userOld);
    }

    [Benchmark]
    public object All_ConfigV6()
    {
        return New.BenchmarkHelper.EvaluateAll(this.evaluationServicesNew, this.userNew);
    }
}
