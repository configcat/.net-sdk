extern alias from_nuget;
extern alias from_project;
using BenchmarkDotNet.Attributes;
using System;

namespace ConfigCatClient.Benchmarks
{
    [MemoryDiagnoser]
    public class GetValueBenchmarks
    {
        private readonly from_project::ConfigCat.Client.IConfigCatClient newClient = from_project::ConfigCat.Client.ConfigCatClientBuilder
            .Initialize("rv3YCMKenkaM7xkOCVQfeg/-I_w49WSQUWdZypPPM4Yyg")
            .WithManualPoll()
            .WithBaseUrl(new Uri("https://test-cdn-global.configcat.com"))
            .Create();

        private readonly from_nuget::ConfigCat.Client.IConfigCatClient oldClient = from_nuget::ConfigCat.Client.ConfigCatClientBuilder
            .Initialize("rv3YCMKenkaM7xkOCVQfeg/-I_w49WSQUWdZypPPM4Yyg")
            .WithManualPoll()
            .WithBaseUrl(new Uri("https://test-cdn-global.configcat.com"))
            .Create();

        private from_project::ConfigCat.Client.User newUser = new from_project::ConfigCat.Client.User("test@test.com");
        private from_nuget::ConfigCat.Client.User oldUser = new from_nuget::ConfigCat.Client.User("test@test.com");

        [GlobalSetup]
        public void Setup()
        {
            this.oldClient.ForceRefresh();
            this.newClient.ForceRefresh();
        }

        [Benchmark(Baseline = true)]
        public object OldGetValue()
        {
            return this.oldClient.GetValue("asd", false, oldUser);
        }

        [Benchmark]
        public object NewGetValue()
        {
            return this.newClient.GetValue("asd", false, newUser);
        }
    }
}
