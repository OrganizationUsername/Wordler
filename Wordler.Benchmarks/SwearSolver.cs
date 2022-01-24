namespace Wordler.Benchmarks;

public class SwearSolver
{
    //Inspired by: https://langproc.substack.com/p/information-theoretic-analysis-of
    private string[][] wordlist;
    private Dictionary<char, HashSet<string>>[] map;
    private Dictionary<char, HashSet<string>>[][] directMap;

    public SwearSolver(int runCount, bool parallel)
    {
        if (!parallel)
        {
            runCount = 1;
        }

        this.wordlist = new string[runCount][];
        this.map = new Dictionary<char, HashSet<string>>[runCount];
        this.directMap = new Dictionary<char, HashSet<string>>[runCount][];
        for (var run = 0; run < runCount; run++)
        {
            this.wordlist[run] = ReservedList.AbelWords.ToList().ToArray();
            this.map[run] = new Dictionary<char, HashSet<string>>();
            this.directMap[run] = new Dictionary<char, HashSet<string>>[5];

            for (var i = 0; i < 5; i++)
            {
                this.directMap[run][i] = new Dictionary<char, HashSet<string>>();
            }

            for (var c = 'a'; c <= 'z'; c++)
            {
                map[run][c] = new HashSet<string>();
                directMap[run][0][c] = new HashSet<string>();
                directMap[run][1][c] = new HashSet<string>();
                directMap[run][2][c] = new HashSet<string>();
                directMap[run][3][c] = new HashSet<string>();
                directMap[run][4][c] = new HashSet<string>();
            }

            foreach (var word in wordlist[run])
            {
                for (var i = 0; i < 5; i++)
                {
                    map[run][word[i]].Add(word);
                    directMap[run][i][word[i]].Add(word);
                }
            }
        }
    }

    public void Run(int runCount, bool parallel)
    {
        if (parallel)
        {
            Parallel.For(0, runCount, run =>
            {
                Solve(run, parallel);
            });
        }
        else
        {
            for (var run = 0; run < runCount; run++)
            {
                Solve(run, parallel);
            }
        }
    }

    public void Solve(int run, bool parallel)
    {
        var rand = new Random();

        if (!parallel)
        {
            run = 0;
        }

        var prompt = wordlist[run][rand.Next() % wordlist[run].Length];
        var nextGuessPool = wordlist[run].ToHashSet();
        var correct = new char[5];
        var proximate = new HashSet<char>();
        var guess = wordlist[run][rand.Next() % wordlist[run].Length];
        var attempt = 1;

        while (!guess.Equals(prompt) || attempt == 7)
        {
            for (var i = 0; i < 5; i++)
            {
                if (guess[i] == prompt[i])
                {
                    correct[i] = guess[i];
                }
                else if (prompt.Any(c => guess[i] == c))
                {
                    proximate.Add(guess[i]);
                    nextGuessPool.ExceptWith(directMap[run][i][guess[i]]);
                }
                else
                {
                    nextGuessPool.ExceptWith(map[run][guess[i]]);
                    nextGuessPool.ExceptWith(directMap[run][i][guess[i]]);
                }
            }

            for (var i = 0; i < 5; i++)
            {
                if (correct[i] != 0)
                {
                    nextGuessPool.IntersectWith(directMap[run][i][correct[i]]);
                }
            }

            if (proximate.Any())
            {
                foreach (var c in proximate)
                {
                    nextGuessPool.IntersectWith(map[run][c]);
                }
            }

            guess = nextGuessPool.ToArray()[rand.Next() % nextGuessPool.Count];
            attempt++;
        }
    }
}