using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wordler.Core
{
    public struct Solver
    {
        private long _startMemory;
        private string _result;
        private int _maxDiversity = 5;
        private readonly int[] _diversityCharacters = new int[26];
        private int _runningDiversity;
        private int _currentDiversity;
        private int _winningIndex;
        (int bad, int wrong, int good)[] letterResults = new (int, int, int)[26];//Get a unique list of letters
        public char[] goodLetterPositions = new char[5];
        public char[] badLetterPositions = new char[5];
        (char letter, int minCount, int maxCount)[] letterCountTuple = new (char letter, int minCount, int maxCount)[5];
        private (char letter, int bad, int wrong, int good)[] trimList = new (char letter, int bad, int wrong, int good)[5];

        public static List<string> GetLines() => File.ReadAllLines("FiveLetterWords.txt").ToList();

        public static void GetAllocations(long startMemory, string data)
        {
            var endMemory = GC.GetAllocatedBytesForCurrentThread();
            var temp = (endMemory - startMemory) / 1024.0 / 1024.0;
            Trace.WriteLine($"{data}: {endMemory} - {startMemory}= {Math.Round(temp, 3)} mb.");
        }
        /////*GetAllocations(StartMemory, Log());*/
        public static string Log([CallerFilePath] string file = null, [CallerLineNumber] int line = 0) => $" {Path.GetFileName(file)}, {line}";

        public string TryAnswersRemove(int guessesRemaining1, IList<string> wordList, string wordToGuess, bool outPut)
        {
            string mostDiverseWord;
            //_startMemory = GC.GetAllocatedBytesForCurrentThread();
            _result = "     ";
            Array.Clear(_diversityCharacters);
            byte[,] AlreadyForbidden = new byte[26, 5];
            byte[] AlreadyRequired = new byte[5];
            //byte[,] AlreadyRequired = new byte[26, 5];

            while (guessesRemaining1 > 0 && _result.Any(x => x != 'G'))
            {
                if (guessesRemaining1 < 6)
                {
                    //var sw = new Stopwatch();
                    //sw.Start();
                    int mostDiverseWordIndex = PrunePossibleWords(wordList, letterCountTuple, goodLetterPositions, badLetterPositions);

                    //Trace.WriteLine($"Guesses remaining: {guessesRemaining1}, target={wordToGuess}, prune time: {sw.Elapsed.TotalMilliseconds}");

                    mostDiverseWord = wordList[mostDiverseWordIndex];
                    wordList[mostDiverseWordIndex] = null;
                    _maxDiversity = Math.Min(_maxDiversity, _runningDiversity);
                }
                else
                {
                    mostDiverseWord = wordList[41];
                    wordList[41] = null;
                    //_maxDiversity = Math.Min(_maxDiversity, _runningDiversity);
                    ////Diversity word
                    //_winningIndex = 0;
                    //_runningDiversity = 0;
                    //mostDiverseWord = default;
                    //for (var index = 0; index < wordList.Count; index++)
                    //{
                    //    var word = wordList[index];
                    //    if (word is null) continue;
                    //    Array.Clear(_diversityCharacters);
                    //    for (var i = 0; i < word.Length; i++)
                    //    {
                    //        var c = word[i];
                    //        _diversityCharacters[c - 'a']++;
                    //    }

                    //    _currentDiversity = 0;
                    //    for (var i = 0; i < _diversityCharacters.Length; i++)
                    //    {
                    //        var c = _diversityCharacters[i];
                    //        if (c != 0) _currentDiversity++;
                    //    }

                    //    if (_currentDiversity > _runningDiversity)
                    //    {
                    //        _winningIndex = index;
                    //        _runningDiversity = _currentDiversity;
                    //        mostDiverseWord = word;
                    //        if (_currentDiversity == _maxDiversity)
                    //        {
                    //            break;
                    //        }
                    //    }
                    //}

                    //if (mostDiverseWord is null)
                    //{
                    //    mostDiverseWord = wordList[_winningIndex];
                    //    wordList[_winningIndex] = null;
                    //    _maxDiversity = Math.Min(_maxDiversity, _runningDiversity);
                    //}
                }

                Array.Clear(letterCountTuple);
                Array.Clear(trimList);
                Array.Clear(goodLetterPositions);
                Array.Clear(badLetterPositions);

#if DEBUG
                if (wordList.Count(x => x is not null) == 0 && wordToGuess != mostDiverseWord)
                {
                    Debugger.Break();
                }
#endif

#if DEBUG

                if (outPut) { Console.WriteLine($"RoboGuess: {mostDiverseWord} out of {wordList.Count(c => c is not null) + 1} words."); }
#endif
                _result = EvaluateResponse(mostDiverseWord, wordToGuess);

                if (_result == "     ") { continue; }

                for (int i = 0; i < mostDiverseWord.Length; i++) //Very small loop.
                {
                    var index = mostDiverseWord[i] - 'a';
                    letterResults[index] = new(
                    letterResults[index].bad + (_result[i] == 'X' ? 1 : 0),
                    letterResults[index].wrong + (_result[i] == 'Y' ? 1 : 0),
                    letterResults[index].good + (_result[i] == 'G' ? 1 : 0)
                    );

                    //if (_result[i] == 'Y') { badLetterPositions[i] = mostDiverseWord[i]; }
                    //if (_result[i] == 'G') { goodLetterPositions[i] = mostDiverseWord[i]; }
                    if (_result[i] == 'Y' && AlreadyForbidden[index, i] == 0) { badLetterPositions[i] = mostDiverseWord[i]; AlreadyForbidden[index, i] = 1; }
                    //if (_result[i] == 'G' && AlreadyRequired[index, i] == 0) { goodLetterPositions[i] = mostDiverseWord[i]; AlreadyRequired[index, i] = 1; }
                    if (_result[i] == 'G' && AlreadyRequired[i] == 0) { goodLetterPositions[i] = mostDiverseWord[i]; AlreadyRequired[i] = 1; }
                }

                int maxTempIndex = 0;
                for (int i = 0; i < letterResults.Length; i++)
                {
                    if (letterResults[i].bad + letterResults[i].wrong + letterResults[i].good > 0)
                    {
                        trimList[maxTempIndex] = ((char)('a' + i), letterResults[i].bad, letterResults[i].wrong, letterResults[i].good);
                        letterResults[i] = (0, 0, 0);
                        maxTempIndex++;
                    }
                }

                var letterCountTupleCount = 0;
                for (int i = 0; i < maxTempIndex; i++)
                {
                    int upperLimit = int.MaxValue;
                    if (trimList[i].bad > 0)
                    { //Then we know the upper limit
                        upperLimit = trimList[i].good + trimList[i].wrong;
                    }
                    var lowerLimit = trimList[i].good + trimList[i].wrong;
                    letterCountTuple[letterCountTupleCount] = ((trimList[i].letter, lowerLimit, upperLimit));
                    letterCountTupleCount++;
                }

                guessesRemaining1--;
#if DEBUG
                if (outPut) { Console.WriteLine(new string(_result.ToArray())); }
#endif
            }
            return _result;
        }

        public int PrunePossibleWords(
            IList<string> wordList,
            (char letter, int minCount, int maxCount)[] letterCountTuple,
            char[] goodLetterPositions,
            char[] badLetterPositions
        )
        {
            int count;
            _currentDiversity = 0;
            _winningIndex = 0;
            _runningDiversity = 0;
#if DEBUG
            var wordsDeletedByletterCountTuple = 0;
            var wordsDeltedByPosition = 0;
#endif

            for (var i = 0; i < wordList.Count; i++)
            {
                var word = wordList[i];
                if (word is null) continue;
                for (var index = 0; index < 5; index++)
                {
                    if (badLetterPositions[index] != '\0')
                    {
                        if (word[index] == badLetterPositions[index])
                        {
                            word = null;
                            wordList[i] = null;
#if DEBUG
                            wordsDeltedByPosition++;
#endif
                            break;
                        }
                    }
                    if (goodLetterPositions[index] != '\0')
                    {
                        if (word[index] != goodLetterPositions[index])
                        {
                            word = null;
                            wordList[i] = null;
#if DEBUG
                            wordsDeltedByPosition++;
#endif
                            break;
                        }
                    }
                }

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
#if DEBUG
                        wordsDeletedByletterCountTuple++;
#endif
                        break;
                    }
                }

                //Get most varied word
                if (word is null) continue;
                if (_currentDiversity == _maxDiversity) continue;
                Array.Clear(_diversityCharacters);
                for (var j = 0; j < word.Length; j++)
                {
                    var c = word[j];
                    _diversityCharacters[c - 'a']++;
                }
                _currentDiversity = 0;
                for (var j = 0; j < _diversityCharacters.Length; j++)
                {
                    var c = _diversityCharacters[j];
                    if (c != 0) _currentDiversity++;
                }
                if (_currentDiversity > _runningDiversity)
                {
                    _winningIndex = i;
                    _runningDiversity = _currentDiversity;
                }
            }
#if DEBUG
            Trace.WriteLine($"Words deleted by letterCount: {wordsDeletedByletterCountTuple} with {letterCountTuple.Length} letter filters. {string.Join(",", letterCountTuple.Select(l => $"{l.letter} {l.minCount}=>{(l.maxCount > 5 ? -1 : l.maxCount)}"))})");
            Trace.WriteLine($"Words deleted by Position: {wordsDeltedByPosition} with '{new string(badLetterPositions.Select(x => x == '\0' ? ' ' : x).ToArray())}' bad and '{new string(goodLetterPositions.Select(x => x == '\0' ? ' ' : x).ToArray())}' good.");
#endif
            return _winningIndex;
        }

        public string EvaluateResponse(string guessLetters, string targetWord)
        {
            var result = new[] { ' ', ' ', ' ', ' ', ' ' };
            if (guessLetters.Length != 5) return "     ";
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
            return new string(result);
        }
    }
}