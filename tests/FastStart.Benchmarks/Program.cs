using BenchmarkDotNet.Running;

namespace FastStart.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<SearchBenchmarks>();
    }
}
