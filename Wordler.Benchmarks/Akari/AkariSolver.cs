namespace Wordler.Akari;

using System.Runtime.InteropServices;
using Wordler.Benchmarks;

public class AkariSolver
{
    [DllImport("Akari/WordleSolverDLL.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern void test();

    [DllImport("Akari/WordleSolverDLL.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern void loadWords(string[] words, int wordCount);

    [DllImport("Akari/WordleSolverDLL.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern int solve(int count);

    [DllImport("Akari/WordleSolverDLL.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern int solveParallel(int count);

    public void Initialize()
    {
        test();
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
}