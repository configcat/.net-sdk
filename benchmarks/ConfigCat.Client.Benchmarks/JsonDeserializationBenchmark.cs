extern alias from_nuget;
extern alias from_project;
using BenchmarkDotNet.Attributes;
using System;

namespace ConfigCat.Client.Benchmarks;

[MemoryDiagnoser]
public class JsonDeserializationBenchmark
{
    private readonly from_project::ConfigCat.Client.IConfigCatClient newClient = from_project::ConfigCat.Client.ConfigCatClient.Get("rv3YCMKenkaM7xkOCVQfeg/-I_w49WSQUWdZypPPM4Yyg", o =>
    {
        o.PollingMode = from_project::ConfigCat.Client.PollingModes.ManualPoll;
        o.BaseUrl = new Uri("https://test-cdn-global.configcat.com");
    });

    private readonly from_nuget::ConfigCat.Client.IConfigCatClient oldClient = from_nuget::ConfigCat.Client.ConfigCatClient.Get("rv3YCMKenkaM7xkOCVQfeg/-I_w49WSQUWdZypPPM4Yyg", o =>
    {
        o.PollingMode = from_nuget::ConfigCat.Client.PollingModes.ManualPoll;
        o.BaseUrl = new Uri("https://test-cdn-global.configcat.com");
    });

    private readonly from_project::ConfigCat.Client.User newUser = new("test@test.com");
    private readonly from_nuget::ConfigCat.Client.User oldUser = new("test@test.com");

    [GlobalSetup]
    public void Setup()
    {
        this.oldClient.ForceRefresh();
        this.newClient.ForceRefresh();
    }

    [Benchmark(Baseline = true)]
    public object Old()
    {
        return this.oldClient.GetValue("asd", false, this.oldUser);
    }

    [Benchmark]
    public object New()
    {
        return this.newClient.GetValue("asd", false, this.newUser);
    }
}
