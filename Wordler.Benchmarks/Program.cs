using BenchmarkDotNet.Running;

namespace Wordler.Benchmarks;

public class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<Benchmark>();
    }
}