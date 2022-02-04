using BenchmarkDotNet.Attributes;
using Wordler.Akari;
using Wordler.Core;

namespace Wordler.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class Benchmark
{
    [Params(1, 10, 100, 1_000)]
    public int Count { get; set; }
    public SwearSolver ss { get; set; }
    public SwearSolver ssp { get; set; }
    public AkariSolver akariSolver;
    public CameronAavik.Wordler.Solver.Tree tree;

    private List<string> _allWords = new();
    private List<string> _randomWords = new();
    private List<string> someWords = new();
    private string[] AllWordsArray;
    Solver solver = new Solver();
    private uint[] intWords;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _allWords = Solver.GetLines();
        AllWordsArray = _allWords.ToArray();
        var ran = new Random(2);
        _randomWords = _allWords.OrderBy(l => ran.NextDouble()).Take(Count).ToList();
        someWords = _allWords.Take(Count).ToList();
        ss = new SwearSolver(Count, false);
        ssp = new SwearSolver(Count, true);
        tree = CameronAavik.Wordler.Solver.BuildGuessTree(_allWords, _allWords);
        solver = new Solver();
        akariSolver = new();
        akariSolver.Initialize();
        intWords = new uint[AllWordsArray.Length];
        for (var i = 0; i < AllWordsArray.Length; i++) { intWords[i] = Solver.StringToInt(AllWordsArray[i]); }
    }

    [Benchmark]
    public string NaiveSolvePreProcessed()
    {
        foreach (var s in someWords)
        {
            solver.TryAnswersRemove(6, AllWordsArray, s, false, intWords);
        }
        return "";
    }

    [Benchmark(Baseline = true)]
    public string NaiveSolve()
    {
        var intWords2 = new uint[AllWordsArray.Length];
        for (var i = 0; i < AllWordsArray.Length; i++) { intWords2[i] = Solver.StringToInt(AllWordsArray[i]); }
        foreach (var s in someWords)
        {
            solver.TryAnswersRemove(6, AllWordsArray, s, false, intWords2);
        }
        return "";
    }

    //[Benchmark]
    public string CsaSolver()
    {
        var runtimeTree = CameronAavik.Wordler.Solver.BuildGuessTree(_allWords, _allWords);

        int maxSteps = 0;
        int total = 0;
        int numFailed = 0;
        foreach (var word in someWords)
        {
            int steps = CameronAavik.Wordler.Solver.Run(word, runtimeTree);
            total += steps;
            maxSteps = Math.Max(maxSteps, steps);
            if (steps > 6) { numFailed++; }
        }

        return "";
    }

    [Benchmark]
    public string CsaPreProcessedSolver()
    {
        int maxSteps = 0;
        int total = 0;
        int numFailed = 0;
        foreach (var word in someWords)
        {
            int steps = CameronAavik.Wordler.Solver.Run(word, tree);
            total += steps;
            maxSteps = Math.Max(maxSteps, steps);
            if (steps > 6) { numFailed++; }
        }

        return "";
    }

    //[Benchmark]
    public string SwearSolver()
    {
        var runtimeSolver = new SwearSolver(Count, false);
        runtimeSolver.Run(Count, false);
        return "";
    }

    [Benchmark]
    public string SwearPreProcessedSolver()
    {
        ss.Run(Count, false);
        return "";
    }

    //[Benchmark]
    public string SwearPreProcessedSolverParallel()
    {
        ssp.Run(Count, true);
        return "";
    }

    [Benchmark]
    public int AkariSolver() => akariSolver.Solve(Count);

    //[Benchmark]
    public int AkariSolverParallel() => akariSolver.SolveParallel(Count);
}