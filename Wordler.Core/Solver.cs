using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wordler.Core
{
    public struct Solver
    {
        private readonly int[] _indices = new int[5];
        private int _letterCount;
        private Dictionary<char, int> tempDictionary = new();
        public long StartMemory;
        public const char Good = 'G';
        public const char BadPosition = 'Y';
        public const char Bad = 'X';


        public static List<string> GetLines()
        {
            return File.ReadAllLines("FiveLetterWords.txt").ToList();
        }

        public static void GetAllocations(long startMemory, string data)
        {
            var endMemory = GC.GetAllocatedBytesForCurrentThread();
            var temp = (endMemory - startMemory) / 1024.0 / 1024.0;
            Trace.WriteLine($"{data}: {endMemory} - {startMemory}= {Math.Round(temp, 3)} mb.");
        }

        public static string Log([CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            return $" {Path.GetFileName(file)}, {line}";
        }

        public char[] TryAnswersRemove(int guessesRemaining1, IList<string> wordList, string wordToGuess, bool outPut)
        {
            StartMemory = GC.GetAllocatedBytesForCurrentThread();
            var knownPosition = new char[5];
            var requiredLettersDictionary = new Dictionary<char, int>();
            for (var c = 'a'; c <= 'z'; c++) { requiredLettersDictionary.Add(c, 0); }

            var requiredLetters = new int[26];
            var maxAllowedLetters = new int[26];
            for (var i = 0; i < maxAllowedLetters.Length; i++) { maxAllowedLetters[i] = int.MaxValue; }

            var forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };

            var result = new char[] { ' ', ' ', ' ', ' ', ' ' };
            List<char> guess;
            var maxDiversity = 5;
            var diversityCharacters = new int[26];
            string mostDiverseWord = null;
            var runningDiversity = 0;
            var currentDiversity = 0;
            var winningIndex = 0;


            GetAllocations(StartMemory, $"Before  Loop:" + Log());

            while (guessesRemaining1 > 0 && (result.Any(x => x != 'G')))
            {
                PrunePossibleWords(wordList, requiredLettersDictionary, knownPosition, maxAllowedLetters, forbiddenLetterPositions);
                knownPosition = new char[5];
                forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };
                maxAllowedLetters = new int[26];
                for (var i = 0; i < maxAllowedLetters.Length; i++) { maxAllowedLetters[i] = int.MaxValue; }

                ///*GetAllocations(StartMemory, Log());*/
                if (!wordList.Any()) return Array.Empty<char>();


                GetAllocations(StartMemory, $"Before  Sort:" + Log());

                winningIndex = 0;
                runningDiversity = 0;
                mostDiverseWord = default;
                for (var index = 0; index < wordList.Count; index++)
                {
                    Array.Clear(diversityCharacters);
                    var g = wordList[index];
                    for (var i = 0; i < g.Length; i++)
                    {
                        var c = g[i];
                        diversityCharacters[c - 'a']++;
                    }

                    currentDiversity = 0;
                    for (var i = 0; i < diversityCharacters.Length; i++)
                    {
                        var c = diversityCharacters[i];
                        if (c != 0) currentDiversity++;
                    }

                    if (currentDiversity > runningDiversity)
                    {
                        winningIndex = index;
                        runningDiversity = currentDiversity;
                        mostDiverseWord = g;
                        if (currentDiversity == maxDiversity)
                        {
                            wordList.RemoveAt(winningIndex);
                            break;
                        }
                    }
                }

                if (mostDiverseWord is null)
                {
                    mostDiverseWord = wordList[winningIndex];
                    wordList.RemoveAt(winningIndex);
                }

                guess = mostDiverseWord.ToList();

                GetAllocations(StartMemory, $"After   Sort:" + Log());

                if (outPut) { Console.WriteLine($"RoboGuess: {new(guess.ToArray())} out of {wordList.Count + 1} words."); }
                ///*GetAllocations(StartMemory, Log());*/
                result = EvaluateResponse(guess, wordToGuess);
                ///*GetAllocations(StartMemory, Log());*/
                if (result.All(c => c == ' ')) { continue; }

                var guessHash = guess.ToHashSet();
                ///*GetAllocations(StartMemory, Log());*/
                foreach (var c in guessHash)
                {
                    Array.Clear(_indices);
                    var arrayIndex = 0;
                    for (var index = 0; index < guess.Count; index++)
                    {
                        if (guess[index] == c) { _indices[arrayIndex] = index; arrayIndex++; }
                    }

                    _letterCount = arrayIndex;
                    var plausible = false;

                    for (var index = 0; index < arrayIndex; index++)
                    {
                        var i = _indices[index];
                        if (result[i] != Bad) continue;
                        plausible = true;
                        _letterCount--;
                    }

                    if (plausible && _letterCount >= 0)
                    {
                        maxAllowedLetters[c - 'a'] = Math.Min(maxAllowedLetters[c - 'a'], _letterCount);
                    }
                }
                ///*GetAllocations(StartMemory, Log());*/
                for (var i = 0; i < result.Length; i++)
                {
                    if (result[i] != Good)
                    {
                        forbiddenLetterPositions[i].Add(guess[i]);
                    }
                }
                tempDictionary.Clear();
                ///*GetAllocations(StartMemory, Log());*/
                for (var i = 0; i < result.Length; i++)
                {
                    if (result[i] == BadPosition || result[i] == Good)
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
                        requiredLetters[key - 'a'] = Math.Max(requiredLetters[key - 'a'], value);
                        requiredLettersDictionary[key] = Math.Max(requiredLettersDictionary[key], value);
                    }

                    if (result[i] == Good)
                    {
                        knownPosition[i] = guess[i];
                    }
                }
                ///*GetAllocations(StartMemory, Log());*/
                guessesRemaining1--;
                if (outPut)
                {
                    Console.WriteLine(new string(result.ToArray()));
                }
            }
            return result;
        }

        public void PrunePossibleWords(
            IList<string> wordList,
            Dictionary<char, int> requiredLetters,
            char[] knownPositionDictionary,
            int[] forbiddenLetters,
            List<char>[] forbiddenLetterPositions)
        {
            //ToDo: Some of the heuristics don't need to be run.
            //i.e. : The 1st character cannot be 'a', 'e', and 'o', then later the 1st character is 'r'.
            //Right now, it iterates through the list for the 1st heuristic even though it's already been pruned.

            var necessaryLetters = requiredLetters.Where(l => l.Value > 0).Select(l => l.Key).ToList();
            var tempStartMemory = GC.GetAllocatedBytesForCurrentThread();
            //GetAllocations(tempStartMemory, Log());

            for (var index = 0; index < necessaryLetters.Count; index++)
            {
                var n = necessaryLetters[index];
                for (var i = wordList.Count - 1; i >= 0; i--)
                {
                    var word = wordList[i];
                    if (!word.Contains(n))
                    {
                        wordList.RemoveAt(i);
                    }
                }

                //wordList.RemoveAll(p => !p.Contains(n));
            }

            //GetAllocations(tempStartMemory, Log());

            //foreach (var n in knownPositionDictionary)
            //{
            //    wordList.RemoveAll(p => p[n.Key] != n.Value);
            //}

            for (var index = 0; index < knownPositionDictionary.Length; index++)
            {
                var n = knownPositionDictionary[index];
                if (n is default(char)) { continue; }

                for (var i = wordList.Count - 1; i >= 0; i--)
                {
                    var word = wordList[i];
                    if (word[index] != n)
                    {
                        wordList.RemoveAt(i);
                    }
                }
            }

            //GetAllocations(tempStartMemory, Log());

            for (var index = 0; index < forbiddenLetters.Length; index++)
            {
                var n = forbiddenLetters[index];
                //wordList.RemoveAll(p => p.Count(c => c == n.Key) > n.Value);

                for (var i = wordList.Count - 1; i >= 0; i--)
                {
                    var word = wordList[i];
                    var count = 0;
                    for (var j = 0; j < word.Length; j++)
                    {
                        if (word[j] == index + 'a') { count++; }
                    }

                    if (count > n)
                    {
                        wordList.RemoveAt(i);
                    }
                }
            }

            //GetAllocations(tempStartMemory, Log());

            for (var n = 0; n < forbiddenLetterPositions.Length; n++)
            {
                for (var i = wordList.Count - 1; i >= 0; i--)
                {
                    var word = wordList[i];
                    if (forbiddenLetterPositions[n].Contains(wordList[i][n]))
                    {
                        wordList.RemoveAt(i);
                    }
                }
            }
            //GetAllocations(StartMemory, Log());
        }

        public char[] EvaluateResponse(List<char> guessLetters, string targetWord)
        {
            ///*GetAllocations(StartMemory, Log());*/
            var result = new char[] { ' ', ' ', ' ', ' ', ' ' };
            if (guessLetters.Count != 5) return Array.Empty<char>();
            var answers = targetWord.ToList();
            ///*GetAllocations(StartMemory, Log());*/
            for (var i = 0; i < 5; i++)
            {
                if (guessLetters[i] == targetWord[i])
                {
                    result[i] = 'G';
                    answers[i] = ' ';
                }
            }
            ///*GetAllocations(StartMemory, Log());*/
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