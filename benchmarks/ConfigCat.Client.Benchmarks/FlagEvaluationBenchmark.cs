extern alias from_nuget;
extern alias from_project;

using System;
using BenchmarkDotNet.Attributes;
using from_project::ConfigCat.Client.Versioning;

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
        var parsedVersion = SemVersion.Parse("1.1.1");
        this.userNew = new("Cat") { Email = "cat@configcat.com", Custom = { ["Version"] = parsedVersion, ["Number"] = "1" } };
    }

    [Params(false, true)]
    public bool LogInfo { get; set; }

    [Benchmark]
    public bool Basic_v9()
    {
        return Old.BenchmarkHelper.Evaluate(this.evaluationServicesOld, "basicFlag", false).Value;
    }

    [Benchmark]
    public bool Basic_vNext()
    {
        return New.BenchmarkHelper.Evaluate(this.evaluationServicesNew, "basicFlag", false).Value;
    }

    [Benchmark]
    public string Complex_v9()
    {
        return Old.BenchmarkHelper.Evaluate(this.evaluationServicesOld, "complexFlag", "", this.userOld).Value;
    }

    [Benchmark]
    public string Complex_vNext()
    {
        return New.BenchmarkHelper.Evaluate(this.evaluationServicesNew, "complexFlag", "", this.userNew).Value;
    }

    [Benchmark]
    public Array All_v9()
    {
        return Old.BenchmarkHelper.EvaluateAll(this.evaluationServicesOld, this.userOld);
    }

    [Benchmark]
    public Array All_vNext()
    {
        return New.BenchmarkHelper.EvaluateAll(this.evaluationServicesNew, this.userNew);
    }
}
