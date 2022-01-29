namespace CameronAavik.Wordler;

public static class Solver
{
    public static byte CheckGuessAgainstWord(string guess, string word)
    {
        int score = 0;

        Span<byte> freq = stackalloc byte[26];
        int offset = 2;
        for (int i = 0; i < 5; i++)
        {
            char wordChar = word[i];
            if (guess[i] == wordChar)
                score += offset;
            else
                freq[wordChar - 'a']++;

            offset *= 3;
        }

        offset = 1;
        for (int i = 0; i < 5; i++)
        {
            char guessChar = guess[i];
            if (guessChar != word[i] && freq[guessChar - 'a'] > 0)
            {
                freq[guessChar - 'a']--;
                score += offset;
            }

            offset *= 3;
        }

        return (byte)score;
    }


    public static string MakeGuess(List<string> remainingWords, List<string> allWords)
    {
        if (remainingWords.Count <= 2)
            return remainingWords[0];

        int bestGuessMaxSize = int.MaxValue;
        string bestGuess = remainingWords[0];

        Span<int> guessResultCounts = stackalloc int[243];
        foreach (var word1 in allWords)
        {
            guessResultCounts.Clear();
            int maxGroupSize = 0;
            foreach (var word2 in remainingWords)
            {
                int guessResult = CheckGuessAgainstWord(word1, word2);
                int newResultCount = ++guessResultCounts[guessResult];
                if (newResultCount > maxGroupSize)
                {
                    maxGroupSize = newResultCount;
                    if (maxGroupSize >= bestGuessMaxSize)
                        break;
                }
            }

            if (maxGroupSize < bestGuessMaxSize)
            {
                bestGuessMaxSize = maxGroupSize;
                bestGuess = word1;
            }
        }

        return bestGuess;
    }

    public readonly record struct Tree(string Guess, Dictionary<byte, Tree> Child);

    public static Tree BuildGuessTree(List<string> remainingWords, List<string> allWords)
    {
        string guess = MakeGuess(remainingWords, allWords);

        var d = new Dictionary<byte, List<string>>();
        foreach (var word in remainingWords)
        {
            byte score = CheckGuessAgainstWord(guess, word);
            if (score == 242)
                continue;

            if (!d.TryGetValue(score, out var list))
                d[score] = list = new List<string>();

            list.Add(word);
        }

        var child = new Dictionary<byte, Tree>();
        foreach ((var match, var words) in d)
            child[match] = BuildGuessTree(words, allWords);

        return new Tree(guess, child);
    }

    public static int Run(string answer, Tree guessTree)
    {
        int steps = 1;
        while (true)
        {
            string guess = guessTree.Guess;
            var score = CheckGuessAgainstWord(guess, answer);
            if (score == 242) // all correct
                return steps;

            guessTree = guessTree.Child[score];
            steps++;
        }
    }
}