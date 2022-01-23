namespace Wordler.Benchmarks;

public class SwearSolver
{
    public void Run(int runCount)
    {
        var rand = new Random();
        var results = new List<(int, bool, TimeSpan)>();
        var wordlist = ReservedList.AbelWords;

        for (var run = 0; run < runCount; run++)
        {
            var prompt = wordlist[rand.Next() % wordlist.Length];
            var start = DateTime.UtcNow;
            var nextGuessPool = wordlist.ToHashSet();
            var correct = new char[5];
            var proximate = new HashSet<char>();
            var guess = wordlist[rand.Next() % wordlist.Length];
            var attempt = 1;
            var map = new Dictionary<char, HashSet<string>>();
            var directMap = new Dictionary<char, HashSet<string>>[5];

            for (var i = 0; i < 5; i++)
            {
                directMap[i] = new Dictionary<char, HashSet<string>>();
            }

            for (var c = 'a'; c <= 'z'; c++)
            {
                map[c] = new HashSet<string>();
                directMap[0][c] = new HashSet<string>();
                directMap[1][c] = new HashSet<string>();
                directMap[2][c] = new HashSet<string>();
                directMap[3][c] = new HashSet<string>();
                directMap[4][c] = new HashSet<string>();
            }

            foreach (var word in wordlist)
            {
                for (var i = 0; i < 5; i++)
                {
                    map[word[i]].Add(word);
                    directMap[i][word[i]].Add(word);
                }
            }

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
                        nextGuessPool = nextGuessPool.Except(directMap[i][guess[i]]).ToHashSet();
                    }
                    else
                    {
                        nextGuessPool = nextGuessPool.Except(map[guess[i]]).Except(directMap[i][guess[i]]).ToHashSet();
                    }
                }

                for (var i = 0; i < 5; i++)
                {
                    if (correct[i] != 0)
                    {
                        nextGuessPool = nextGuessPool.Intersect(directMap[i][correct[i]]).ToHashSet();
                    }
                }

                if (proximate.Any())
                {
                    foreach (var c in proximate)
                    {
                        nextGuessPool = nextGuessPool.Intersect(map[c]).ToHashSet();
                    }
                }

                guess = nextGuessPool.ToArray()[rand.Next() % nextGuessPool.Count];
                attempt++;
            }
            var duration = DateTime.UtcNow - start;
            results.Add((run, attempt != 7, duration));
        }

        foreach (var result in results)
        {
            Console.WriteLine($"{result.Item1},{result.Item2},{result.Item3:G}");
        }
    }
}