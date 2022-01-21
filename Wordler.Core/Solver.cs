namespace Wordler.Core
{
    public static class Solver
    {
        public static List<char>? TryAnswers(int guessesRemaining1, bool b, List<string> list, string s)
        {
            var dictionary = new Dictionary<char, int>();
            var forbiddenLetters = new Dictionary<char, int>();
            for (var c = 'a'; c <= 'z'; c++) { dictionary.Add(c, 0); forbiddenLetters.Add(c, int.MaxValue); }

            var knownPositions = new Dictionary<int, char>();
            var forbiddenLetterPositions = new Dictionary<int, List<char>>();
            foreach (var i in Enumerable.Range(0, 5)) { forbiddenLetterPositions.Add(i, new()); }

            var result = new List<char>() { ' ', ' ', ' ', ' ', ' ' };
            List<char> guess;
            while (guessesRemaining1 > 0 && (result is null || result.Any(x => x != 'G')))
            {
                if (b)
                {
                    guess = Console.ReadLine()?.ToList() ?? new List<char>();
                }
                else
                {
                    var necessaryLetters = dictionary.Where(l => l.Value > 0).Select(l => l.Key).ToList();

                    foreach (var n in necessaryLetters)
                    {
                        list = list.Where(p => p.Contains(n)).ToList();
                    }

                    foreach (var n in knownPositions)
                    {
                        list = list.Where(p => p[n.Key] == n.Value).ToList();
                    }

                    foreach (var n in forbiddenLetters)
                    {
                        list = list.Where(p => p.Count(c => c == n.Key) <= n.Value).ToList();
                    }

                    for (var n = 0; n < forbiddenLetterPositions.Count; n++)
                    {
                        list = list.Where(p => !forbiddenLetterPositions[n].Contains(p[n])).ToList();
                    }


                    list = list.OrderByDescending(c => c.Distinct().ToHashSet().Count).ToList();
                    guess = list.First().ToList();

                    list.RemoveAt(0);
                }

                result = Solver.EvaluateResponse(guess, s);
                if (result is null || result.All(c => c == ' '))
                {
                    continue;
                }

                //ToDo: This is where I see if there are more X's for a particular character than guessCharacters of the same type, then I can cap the number of characters of that type are allowed.
                //This only helps if there is at least 1 fail for the character type.

                var guessHash = guess.ToHashSet();

                foreach (var c in guessHash)
                {
                    var indices = new List<int>();

                    for (var index = 0; index < guess.Count; index++)
                    {
                        if (guess[index] == c) { indices.Add(index); }
                    }
                    var letterCount = indices.Count;
                    var plausible = false;
                    for (var index = 0; index < indices.Count; index++)
                    {
                        if (result[indices[index]] == 'X')
                        {
                            plausible = true;
                            letterCount--;
                        }
                    }

                    if (plausible && letterCount >= 0)
                    {
                        forbiddenLetters[c] = Math.Min(forbiddenLetters[c], letterCount);
                    }

                }


                for (var i = 0; i < result.Count; i++)
                {
                    if (result[i] == 'G')
                    {
                        knownPositions[i] = guess[i];
                    }
                    else
                    {
                        forbiddenLetterPositions[i].Add(guess[i]);
                    }
                }

                var tempDictionary = new Dictionary<char, int>();
                for (var i = 0; i < result.Count; i++)
                {
                    if (result[i] == 'Y' || result[i] == 'G')
                    {
                        if (tempDictionary.ContainsKey(guess[i]))
                        {
                            tempDictionary[guess[i]]++;
                        }
                        else
                        {
                            tempDictionary.TryAdd(guess[i], 1);
                        }
                    }

                    foreach (var kvp in tempDictionary)
                    {
                        dictionary[kvp.Key] = Math.Max(dictionary[kvp.Key], kvp.Value);
                    }

                    if (result[i] == 'G')
                    {
                        knownPositions.TryAdd(i, guess[i]);
                    }
                }
                guessesRemaining1--;
            }
            return result;
        }



        public static List<char> EvaluateResponse(List<char> guessLetters, string targetWord)
        {
            var result = new List<char>() { ' ', ' ', ' ', ' ', ' ' };
            if (guessLetters.Count != 5) return null;
            var answers = targetWord.ToList();

            for (var i = 0; i < 5; i++)
            {
                if (guessLetters[i] == targetWord[i])
                {
                    result[i] = 'G';
                    answers[i] = ' ';
                }
            }
            for (var i = 0; i < 5; i++)
            {
                if (result[i] != ' ') { continue; }
                var index = answers.FindIndex(x => x == guessLetters[i]);
                if (index == -1)
                {
                    result[i] = 'X';
                    continue;
                }
                result[i] = 'Y';
                answers[index] = ' ';
            }

            return result;
        }
    }
}