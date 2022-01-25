using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wordler.Core
{
    public struct Solver
    {
        private readonly int[] _indices = new int[5];
        private int _letterCount;
        private readonly Dictionary<char, int> tempDictionary = new();
        private long StartMemory;
        private const char Good = 'G';
        private const char BadPosition = 'Y';
        private const char Bad = 'X';
        private char[] result = new char[] { ' ', ' ', ' ', ' ', ' ' };
        private List<char> guess;
        private int maxDiversity = 5;
        private readonly int[] diversityCharacters = new int[26];
        private string mostDiverseWord = null;
        private int runningDiversity = 0;
        private int currentDiversity = 0;
        private int winningIndex = 0;
        private char[] knownPosition = new char[5];
        private readonly Dictionary<char, int> requiredLettersDictionary = new Dictionary<char, int>();
        private readonly int[] requiredLetters = new int[26];
        private int[] maxAllowedLetters = new int[26];
        private List<char>[] forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };


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
            result = new char[] { ' ', ' ', ' ', ' ', ' ' };
            Array.Clear(diversityCharacters);
            requiredLettersDictionary.Clear();
            for (var c = 'a'; c <= 'z'; c++) { requiredLettersDictionary.Add(c, 0); }
            Array.Clear(requiredLetters);
            Array.Clear(knownPosition);
            Array.Clear(maxAllowedLetters);
            for (var i = 0; i < maxAllowedLetters.Length; i++) { maxAllowedLetters[i] = int.MaxValue; }
            forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };

            while (guessesRemaining1 > 0 && (result.Any(x => x != 'G')))
            {
                PrunePossibleWords(wordList, requiredLettersDictionary, knownPosition, maxAllowedLetters, forbiddenLetterPositions);
                knownPosition = new char[5];
                forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };
                maxAllowedLetters = new int[26];
                for (var i = 0; i < maxAllowedLetters.Length; i++) { maxAllowedLetters[i] = int.MaxValue; }

                ///*GetAllocations(StartMemory, Log());*/
                if (!wordList.Any()) return Array.Empty<char>();

                //GetAllocations(StartMemory, $"Before  Sort:" + Log());

                winningIndex = 0;
                runningDiversity = 0;
                mostDiverseWord = default;
                for (var index = 0; index < wordList.Count; index++)
                {
                    var word = wordList[index];
                    if (word is null) continue;
                    Array.Clear(diversityCharacters);
                    for (var i = 0; i < word.Length; i++)
                    {
                        var c = word[i];
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
                        mostDiverseWord = word;
                        if (currentDiversity == maxDiversity)
                        {
                            word = null;
                            break;
                        }
                    }
                }

                if (mostDiverseWord is null)
                {
                    mostDiverseWord = wordList[winningIndex];
                    wordList[winningIndex] = null;
                    maxDiversity = Math.Min(maxDiversity, runningDiversity);
                }

                guess = mostDiverseWord.ToList();

                //GetAllocations(StartMemory, $"After   Sort:" + Log());

                if (outPut) { Console.WriteLine($"RoboGuess: {new(guess.ToArray())} out of {wordList.Count(c => c is not null) + 1} words."); }
                ///*GetAllocations(StartMemory, Log());*/
                result = EvaluateResponse(mostDiverseWord, wordToGuess);
                ///*GetAllocations(StartMemory, Log());*/
                if (result.All(c => c == ' ')) { continue; }

                var guessHash = guess.ToHashSet(); //ToDo: Replace this with code above that creates an array of unique characters
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

                    foreach ((var key, var value) in tempDictionary)
                    {
                        requiredLetters[key - 'a'] = Math.Max(requiredLetters[key - 'a'], value);
                        requiredLettersDictionary[key] = Math.Max(requiredLettersDictionary[key], value);
                    }

                    if (result[i] == Good)
                    {
                        knownPosition[i] = guess[i];
                    }
                }
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
            IList<char>[] forbiddenLetterPositions)
        {
            int lhs;
            var toCompare = -1;
            int count;
            var necessaryLetters = requiredLetters.Where(l => l.Value > 0).Select(l => l.Key).ToList();
            //var tempStartMemory = GC.GetAllocatedBytesForCurrentThread();
            //GetAllocations(tempStartMemory, Log());

            for (var i = wordList.Count - 1; i >= 0; i--)
            {
                var word = wordList[i];
                if (word is null) continue;
                for (var index = 0; index < necessaryLetters.Count; index++)
                {
                    var n = necessaryLetters[index];

                    if (!word.Contains(n))
                    {
                        wordList[i] = null;
                        word = null;
                        break;
                    }
                }

                if (word is null) continue;
                for (var index = 0; index < knownPositionDictionary.Length; index++)
                {
                    var n = knownPositionDictionary[index];
                    if (n is default(char)) { continue; }
                    if (word[index] != n)
                    {
                        word = null;
                        wordList[i] = null;
                        break;
                    }
                }

                if (word is null) continue;
                for (var n = 0; n < forbiddenLetterPositions.Length; n++)
                {
                    if (forbiddenLetterPositions[n].Contains(word[n]))
                    {
                        word = null;
                        wordList[i] = null;
                        break;
                    }
                }

                if (word is null) continue;
                for (var index = 0; index < forbiddenLetters.Length; index++)
                {
                    var n = forbiddenLetters[index];
                    count = 0;
                    toCompare = index + 'a';

                    if (word[0] == toCompare) count++;
                    if (word[1] == toCompare) count++;
                    if (word[2] == toCompare) count++;
                    if (word[3] == toCompare) count++;
                    if (word[4] == toCompare) count++;

                    //for (var j = 0; j < word.Length; j++)
                    //{
                    //    lhs = word[j];
                    //    if (lhs == toCompare)
                    //    {
                    //        count++;
                    //    }
                    //}

                    if (count > n)
                    {
                        wordList[i] = null;
                        break;
                    }
                }
            }
        }

        public char[] EvaluateResponse(string guessLetters, string targetWord)
        {
            var result = new[] { ' ', ' ', ' ', ' ', ' ' };
            if (guessLetters.Length != 5) return Array.Empty<char>();
            var answers = targetWord.ToArray();

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

                var index = -1;

                for (var index1 = 0; index1 < answers.Length; index1++)
                {
                    if (answers[index1] == guessLetters[i])
                    {
                        index = index1;
                        break;
                    }
                }

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