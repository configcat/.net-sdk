using BenchmarkDotNet.Running;
using System;

namespace ConfigCatClient.Benchmarks
{
    class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<GetValueBenchmarks>();

            Console.ReadKey();
        }
    }
}
