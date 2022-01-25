using System.Text;
using BenchmarkDotNet.Attributes;
using Wordler.Core;

namespace Wordler.Benchmarks;

[MemoryDiagnoser]
public class Benchmark
{
    [Params(/*1,*/ 10/*, 100*/)]
    public int Count { get; set; }
    public SwearSolver ss { get; set; }
    public SwearSolver ssp { get; set; }
    private IEnumerable<char[]> _allWords= new List<char[]>();
    private List<char[]> _randomWords = new();
    [GlobalSetup]
    public void GlobalSetup()
    {
        _allWords = Solver.GetLines().Select(c => c.ToCharArray());
        var ran = new Random(2);
        _randomWords = _allWords.OrderBy(l => ran.NextDouble()).Take(Count).ToList();
        ss = new SwearSolver(Count, false);
        ssp = new SwearSolver(Count, true);
    }

    [Benchmark(Baseline = true)]
    public string NaiveSolve()
    {
        var sb = new StringBuilder();
        var reloadableWords = new List<char[]>();
        for (var index = 0; index < _randomWords.Count; index++)
        {
            char[] s = _randomWords[index];
            reloadableWords.Clear();
            reloadableWords.AddRange(_allWords);
            Solver solver = new Solver();
            sb.Append(new string(solver.TryAnswersRemove(6, reloadableWords, s, false).ToArray()));
        }

        return sb.ToString();
    }

    //[Benchmark]
    public string SwearSolver()
    {
        ss.Run(Count, false);
        return "";
    }

    //[Benchmark]
    public string SwearSolverParallel()
    {
        ssp.Run(Count, true);
        return "";
    }

}