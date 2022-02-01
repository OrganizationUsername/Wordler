#define ENABLE_AKARI_TEST
namespace Wordler.Akari;

using System.Runtime.InteropServices;
using Wordler.Benchmarks;

public class AkariSolver
{
#if ENABLE_AKARI_TEST
    [DllImport("Akari/WordleSolverDLL.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern void loadWords(string[] words, int wordCount);

    [DllImport("Akari/WordleSolverDLL.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern int solve(int count);

    [DllImport("Akari/WordleSolverDLL.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern int solveParallel(int count);

    public void Initialize()
    {
        loadWords(ReservedList.AbelWords, ReservedList.AbelWords.Length);
    }

    public int Solve(int count)
    {
        return solve(count);
    }

    public int SolveParallel(int count)
    {
        return solveParallel(count);
    }

#else
    public void Initialize() { }

    public int Solve(int count) => 0;

    public int SolveParallel(int count) => 0;

#endif
}