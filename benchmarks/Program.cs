using BenchmarkDotNet.Running;
using System;

namespace ConfigCatClient.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        BenchmarkRunner.Run<JsonDeserializationBenchmark>();

        Console.ReadKey();
    }
}
