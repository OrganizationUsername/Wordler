﻿using System.Diagnostics;
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
        public char[] goodLetterPositions = new char[5]; //Should save this somewhere else so it doesn't try to filter on it a second time.
        public char[] badLetterPositions = new char[5];
        (char letter, int minCount, int maxCount)[] letterCountTuple = new (char letter, int minCount, int maxCount)[5];
        private (char letter, int bad, int wrong, int good)[] trimList;

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

            while (guessesRemaining1 > 0 && _result.Any(x => x != 'G'))
            {
                if (guessesRemaining1 < 6)
                {
                    //var sw = new Stopwatch();
                    //sw.Start();
                    int mostDiverseWordIndex = PrunePossibleWords(wordList, letterCountTuple, goodLetterPositions, badLetterPositions);
                    //Trace.WriteLine($"prune time: {sw.Elapsed.TotalMilliseconds}");

                    mostDiverseWord = wordList[mostDiverseWordIndex];
                    wordList[mostDiverseWordIndex] = null;
                    _maxDiversity = Math.Min(_maxDiversity, _runningDiversity);
                }
                else
                {
                    //Diversity word
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

                letterCountTuple = new (char letter, int minCount, int maxCount)[5];
                trimList = new (char, int, int, int)[5];
                goodLetterPositions = new char[5];
                badLetterPositions = new char[5];

#if DEBUG
                if (wordList.Count(x => x is not null) == 0 && wordToGuess != mostDiverseWord)
                {
                    Debugger.Break();
                }
#endif

#if DEBUG

                if (outPut) { Console.WriteLine($"RoboGuess: {new(_guess.ToArray())} out of {wordList.Count(c => c is not null) + 1} words."); }
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

                    if (_result[i] == 'Y') { badLetterPositions[i] = mostDiverseWord[i]; }
                    if (_result[i] == 'G') { goodLetterPositions[i] = mostDiverseWord[i]; }
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
            Array.Clear(_diversityCharacters);

            for (var i = 0; i < wordList.Count; i++)
            {
                var word = wordList[i];
                if (word is null) continue;
                for (var index = 0; index < this.goodLetterPositions.Length; index++)
                {
                    if (this.badLetterPositions[index] != '\0')
                    {
                        if (word[index] == badLetterPositions[index])
                        {
                            word = null;
                            wordList[i] = null;
                            break;
                        }
                    }
                    if (this.goodLetterPositions[index] != '\0')
                    {
                        if (word[index] != this.goodLetterPositions[index])
                        {
                            word = null;
                            wordList[i] = null;
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
                        break;
                    }
                }

                //Get most varied word
                if (word is null) continue;
                if (_currentDiversity == _maxDiversity) continue;
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