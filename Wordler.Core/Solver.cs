using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wordler.Core
{
    public sealed class Solver
    {
        private readonly List<int> _indices = new(5);
        private int _letterCount;
        private Dictionary<char, int> tempDictionary = new();
        private Dictionary<int, char> knownPositions = new();
        public long startMemory = GC.GetAllocatedBytesForCurrentThread();

        public static void GetAllocations(long startMemory, string data)
        {
            var endMemory = GC.GetAllocatedBytesForCurrentThread();
            Trace.WriteLine(data);
            var temp = (endMemory - startMemory) / 1024;
            Trace.WriteLine($"{endMemory} - {startMemory}= {temp} kb.");
        }

        public static string Log([CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            return $" {Path.GetFileName(file)}, {line}";
        }

        public List<char> TryAnswersRemove(int guessesRemaining1, List<string> wordList, string wordToGuess, bool outPut)
        {

            var requiredLettersDictionary = new Dictionary<char, int>();
            var forbiddenLetters = new Dictionary<char, int>();
            for (var c = 'a'; c <= 'z'; c++) { requiredLettersDictionary.Add(c, 0); forbiddenLetters.Add(c, int.MaxValue); }

            var fl = new int[26];

            var forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };

            var PreviousGuesses = new List<string>();
            var result = new List<char>() { ' ', ' ', ' ', ' ', ' ' };
            List<char> guess;

            //GetAllocations(startMemory, Log());

            while (guessesRemaining1 > 0 && (result.Any(x => x != 'G')))
            {
                PrunePossibleWords(wordList, requiredLettersDictionary, knownPositions, fl, forbiddenLetterPositions, PreviousGuesses);
                //GetAllocations(startMemory, Log());
                if (!wordList.Any()) return new();

                guess = wordList.OrderByDescending(c => c.Distinct().Count()).First().ToList();
                PreviousGuesses.Add(new(guess.ToArray()));

                if (outPut) { Console.WriteLine($"RoboGuess: {new(guess.ToArray())} out of {wordList.Count} words."); }
                //GetAllocations(startMemory, Log());
                wordList.RemoveAt(0);
                result = EvaluateResponse(guess, wordToGuess);
                //GetAllocations(startMemory, Log());
                if (result.All(c => c == ' ')) { continue; }

                var guessHash = guess.ToHashSet();
                //GetAllocations(startMemory, Log());
                foreach (var c in guessHash)
                {
                    _indices.Clear();
                    for (var index = 0; index < guess.Count; index++)
                    {
                        if (guess[index] == c) { _indices.Add(index); }
                    }

                    _letterCount = _indices.Count;
                    var plausible = false;

                    foreach (var i in _indices)
                    {
                        if (result[i] != 'X') continue;
                        plausible = true;
                        _letterCount--;
                    }

                    if (plausible && _letterCount >= 0)
                    {
                        forbiddenLetters[c] = Math.Min(forbiddenLetters[c], _letterCount);
                        fl[c - 'a'] = Math.Min(fl[c - 'a'], _letterCount);
                    }
                }
                //GetAllocations(startMemory, Log());
                for (var i = 0; i < result.Count; i++)
                {
                    if (result[i] != 'G')
                    {
                        forbiddenLetterPositions[i].Add(guess[i]);
                    }
                }
                tempDictionary.Clear();
                //GetAllocations(startMemory, Log());
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

                    foreach (var (key, value) in tempDictionary)
                    {
                        requiredLettersDictionary[key] = Math.Max(requiredLettersDictionary[key], value);
                    }

                    if (result[i] == 'G')
                    {
                        knownPositions.TryAdd(i, guess[i]);
                    }
                }
                //GetAllocations(startMemory, Log());
                guessesRemaining1--;
                if (outPut)
                {
                    Console.WriteLine(new string(result.ToArray()));
                }
            }
            return result;
        }

        public void PrunePossibleWords(List<string> wordList, Dictionary<char, int> requiredLetters,
            Dictionary<int, char> knownPositionDictionary, int[] forbiddenLetters,
            List<char>[] forbiddenLetterPositions, List<string> PreviousGuesses)
        {
            var necessaryLetters = requiredLetters.Where(l => l.Value > 0).Select(l => l.Key).ToList();
            var tempStartMemory = GC.GetAllocatedBytesForCurrentThread();
            GetAllocations(tempStartMemory, Log());

            for (var index = 0; index < necessaryLetters.Count; index++)
            {
                var n = necessaryLetters[index];
                //for (var i = wordList.Count - 1; i >= 0; i--)
                //{
                //    var word = wordList[i];
                //    if (!word.Contains(n))
                //    {
                //        wordList.RemoveAt(i); // 272mb for 10
                //    }
                //}

                wordList.RemoveAll(p => !p.Contains(n)); // 272mb for 10
            }

            GetAllocations(tempStartMemory, Log());

            //foreach (var n in knownPositionDictionary)
            //{
            //    wordList.RemoveAll(p => p[n.Key] != n.Value);
            //}

            foreach (var n in knownPositionDictionary)
            {
                for (var i = wordList.Count - 1; i >= 0; i--)
                {
                    var word = wordList[i];
                    if (word[n.Key] != n.Value)
                    {
                        wordList.RemoveAt(i); // 272mb for 10
                    }
                }
            }

            GetAllocations(tempStartMemory, Log());

            for (var index = 0; index < forbiddenLetters.Length; index++)
            {
                var n = forbiddenLetters[index];
                //wordList.RemoveAll(p => p.Count(c => c == n.Key) > n.Value);
                for (var i = wordList.Count - 1; i >= 0; i--)
                {
                    var word = wordList[i];

                    if (word.Count(c => c == n + 'a') > n)
                    {
                        wordList.RemoveAt(i); // 272mb for 10
                    }
                }
            }

            GetAllocations(startMemory, Log()); // ~17mb

            for (var n = 0; n < forbiddenLetterPositions.Length; n++)
            {
                for (var i = wordList.Count - 1; i >= 0; i--)
                {
                    var word = wordList[i];
                    if (forbiddenLetterPositions[n].Contains(wordList[i][n]))
                    {
                        wordList.RemoveAt(i); // 272mb for 10
                    }
                }
            }
            GetAllocations(startMemory, Log());
            wordList.RemoveAll(g => PreviousGuesses.Contains(g));
            GetAllocations(startMemory, Log());
        }

        public List<char> EvaluateResponse(List<char> guessLetters, string targetWord)
        {
            //GetAllocations(startMemory, Log());
            var result = new List<char>() { ' ', ' ', ' ', ' ', ' ' };
            if (guessLetters.Count != 5) return null;
            var answers = targetWord.ToList();
            //GetAllocations(startMemory, Log());
            for (var i = 0; i < 5; i++)
            {
                if (guessLetters[i] == targetWord[i])
                {
                    result[i] = 'G';
                    answers[i] = ' ';
                }
            }
            //GetAllocations(startMemory, Log());
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