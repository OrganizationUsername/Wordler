using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wordler.Core
{
    public struct Solver
    {
        private readonly int[] _indices = new int[5];
        private int _letterCount;
        private readonly Dictionary<char, int> _tempDictionary = new();
        private long _startMemory;
        private const char Good = 'G';
        private const char BadPosition = 'Y';
        private const char Bad = 'X';
        private char[] _result = new char[] { ' ', ' ', ' ', ' ', ' ' };
        private List<char> _guess;
        private int _maxDiversity = 5;
        private readonly int[] _diversityCharacters = new int[26];
        private int _runningDiversity;
        private int _currentDiversity;
        private int _winningIndex;
        private char[] _knownPosition = new char[5];
        private readonly int[] _requiredLetters = new int[26];
        private int[] _maxAllowedLetters = new int[26];
        private List<char>[] _forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };

        public static List<string> GetLines() => File.ReadAllLines("FiveLetterWords.txt").ToList();

        public static void GetAllocations(long startMemory, string data)
        {
            var endMemory = GC.GetAllocatedBytesForCurrentThread();
            var temp = (endMemory - startMemory) / 1024.0 / 1024.0;
            Trace.WriteLine($"{data}: {endMemory} - {startMemory}= {Math.Round(temp, 3)} mb.");
        }

        public static string Log([CallerFilePath] string file = null, [CallerLineNumber] int line = 0) => $" {Path.GetFileName(file)}, {line}";

        public char[] TryAnswersRemove(int guessesRemaining1, IList<string> wordList, string wordToGuess, bool outPut)
        {
            string mostDiverseWord;
            _startMemory = GC.GetAllocatedBytesForCurrentThread();
            _result = new char[] { ' ', ' ', ' ', ' ', ' ' };
            Array.Clear(_diversityCharacters);
            var letterCountTuple = new List<(char letter, int minCount, int maxCount)>();
            Array.Clear(_requiredLetters);
            Array.Clear(_knownPosition);
            Array.Clear(_maxAllowedLetters);
            for (var i = 0; i < _maxAllowedLetters.Length; i++) { _maxAllowedLetters[i] = int.MaxValue; }
            _forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };

            while (guessesRemaining1 > 0 && _result.Any(x => x != 'G'))
            {
                if (guessesRemaining1 < 6)
                {
                    //var sw = new Stopwatch();
                    //sw.Start();
                    PrunePossibleWords(wordList, _knownPosition, _maxAllowedLetters, _forbiddenLetterPositions, letterCountTuple);
                    //Trace.WriteLine($"prune time: {sw.Elapsed.TotalMilliseconds}");
                }
                letterCountTuple.Clear();
                _knownPosition = new char[5];
                _forbiddenLetterPositions = new List<char>[] { new(), new(), new(), new(), new() };
                _maxAllowedLetters = new int[26];
                for (var i = 0; i < _maxAllowedLetters.Length; i++) { _maxAllowedLetters[i] = int.MaxValue; }

                ///*GetAllocations(StartMemory, Log());*/
                if (!wordList.Any()) return Array.Empty<char>();

                //GetAllocations(StartMemory, $"Before  Sort:" + Log());

                { //Diversity word
                    _winningIndex = 0;
                    _runningDiversity = 0;
                    mostDiverseWord = default;
                    for (var index = 0; index < wordList.Count; index++)
                    {
                        var word = wordList[index];
                        if (word is null) continue;
                        Array.Clear(_diversityCharacters);
                        for (var i = 0; i < word.Length; i++)
                        {
                            var c = word[i];
                            _diversityCharacters[c - 'a']++;
                        }

                        _currentDiversity = 0;
                        for (var i = 0; i < _diversityCharacters.Length; i++)
                        {
                            var c = _diversityCharacters[i];
                            if (c != 0) _currentDiversity++;
                        }

                        if (_currentDiversity > _runningDiversity)
                        {
                            _winningIndex = index;
                            _runningDiversity = _currentDiversity;
                            mostDiverseWord = word;
                            if (_currentDiversity == _maxDiversity)
                            {
                                word = null;
                                break;
                            }
                        }
                    }

                    if (mostDiverseWord is null)
                    {
                        mostDiverseWord = wordList[_winningIndex];
                        wordList[_winningIndex] = null;
                        _maxDiversity = Math.Min(_maxDiversity, _runningDiversity);
                    }
                }
                _guess = mostDiverseWord.ToList();

                //GetAllocations(StartMemory, $"After   Sort:" + Log());

                if (outPut) { Console.WriteLine($"RoboGuess: {new(_guess.ToArray())} out of {wordList.Count(c => c is not null) + 1} words."); }
                ///*GetAllocations(StartMemory, Log());*/
                _result = EvaluateResponse(mostDiverseWord, wordToGuess);
                ///*GetAllocations(StartMemory, Log());*/
                if (_result.All(c => c == ' ')) { continue; }

                var guessHash = _guess.ToHashSet(); //ToDo: Replace this with code above that creates an array of unique characters
                ///*GetAllocations(StartMemory, Log());*/
                foreach (var c in guessHash)
                {
                    Array.Clear(_indices);
                    var arrayIndex = 0;
                    for (var index = 0; index < _guess.Count; index++)
                    {
                        if (_guess[index] == c) { _indices[arrayIndex] = index; arrayIndex++; }
                    }

                    _letterCount = arrayIndex;
                    var plausible = false;

                    for (var index = 0; index < arrayIndex; index++)
                    {
                        var i = _indices[index];
                        if (_result[i] != Bad) continue;
                        plausible = true;
                        _letterCount--;
                    }

                    if (plausible && _letterCount >= 0)
                    {
                        var d = c - 'a';
                        letterCountTuple.Add((c, -1, _letterCount));
                        _maxAllowedLetters[d] = Math.Min(_maxAllowedLetters[d], _letterCount);
                        //Trace.WriteLine($"{(c, -1, _letterCount)}, {_maxAllowedLetters[d]}");
                    }
                }
                ///*GetAllocations(StartMemory, Log());*/
                for (var i = 0; i < _result.Length; i++)
                {
                    if (_result[i] != Good) { _forbiddenLetterPositions[i].Add(_guess[i]); }
                }
                _tempDictionary.Clear();
                ///*GetAllocations(StartMemory, Log());*/
                for (var i = 0; i < _result.Length; i++)
                {
                    if (_result[i] == BadPosition || _result[i] == Good)
                    {
                        if (_tempDictionary.ContainsKey(_guess[i])) { _tempDictionary[_guess[i]]++; }
                        else { _tempDictionary.TryAdd(_guess[i], 1); }
                    }

                    foreach ((var key, var value) in _tempDictionary)
                    {
                        letterCountTuple.Add((key, value, int.MaxValue));
                    }

                    if (_result[i] == Good) { _knownPosition[i] = _guess[i]; }
                }
                guessesRemaining1--;
                if (outPut) { Console.WriteLine(new string(_result.ToArray())); }
            }
            return _result;
        }

        public void PrunePossibleWords(
            IList<string> wordList,
            char[] knownPositionDictionary,
            int[] forbiddenLetters,
            IList<char>[] forbiddenLetterPositions,
            List<(char letter, int minCount, int maxCount)> letterCountTuple)
        {
            int count;
            //var tempStartMemory = GC.GetAllocatedBytesForCurrentThread();
            //GetAllocations(tempStartMemory, Log());

            for (var i = wordList.Count - 1; i >= 0; i--)
            {
                var word = wordList[i];
                if (word is null) continue;

                foreach (var tuple in letterCountTuple)
                {
                    count = 0;
                    if (word[0] == tuple.letter) count++;
                    if (word[1] == tuple.letter) count++;
                    if (word[2] == tuple.letter) count++;
                    if (word[3] == tuple.letter) count++;
                    if (word[4] == tuple.letter) count++;

                    if (count > tuple.maxCount || count < tuple.minCount)
                    {
                        word = null;
                        wordList[i] = null;
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
                    var toCompare = index + 'a';

                    if (word[0] == toCompare) count++;
                    if (word[1] == toCompare) count++;
                    if (word[2] == toCompare) count++;
                    if (word[3] == toCompare) count++;
                    if (word[4] == toCompare) count++;

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