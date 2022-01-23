using System.Text;
using BenchmarkDotNet.Attributes;
using Wordler.Core;

namespace Wordler.Benchmarks;

[MemoryDiagnoser]
public class Benchmark
{
    [Params( 10)]
    public int Count { get; set; }
    public SwearSolver ss { get; set; }
    private List<string> _allWords = new();
    private List<string> _randomWords = new();
    [GlobalSetup]
    public void GlobalSetup()
    {
        _allWords = Solver.GetLines();
        var ran = new Random(2);
        _randomWords = _allWords.OrderBy(l => ran.NextDouble()).Take(Count).ToList();
        ss = new SwearSolver(Count);
    }

    //[Benchmark]
    public string NaiveSolve_ToList()
    {
        var sb = new StringBuilder();
        foreach (var s in _randomWords)
        {
            var reloadableWords = _allWords.ToList();

            Solver solver = new Solver();
            sb.Append(new string(solver.TryAnswersRemove(6, reloadableWords, s, false).ToArray()));
        }
        return sb.ToString();
    }

    [Benchmark(Baseline = true)]
    public string NaiveSolve_ClearAddRange()
    {
        var sb = new StringBuilder();
        var reloadableWords = new List<string>();
        foreach (var s in _randomWords)
        {
            reloadableWords.Clear();
            reloadableWords.AddRange(_allWords);
            Solver solver = new Solver();
            sb.Append(new string(solver.TryAnswersRemove(6, reloadableWords, s, false).ToArray()));
        }
        return sb.ToString();
    }

    [Benchmark]
    public string SwearSolver()
    {
        ss.Run(Count);
        return "";
    }

}