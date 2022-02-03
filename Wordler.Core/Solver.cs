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
        (byte bad, byte wrong, byte good)[] letterResults = new (byte, byte, byte)[26];//Get a unique list of letters
        public char[] goodLetterPositions = new char[5];
        public char[] badLetterPositions = new char[5];
        (char letter, byte minCount, byte maxCount)[] letterCountTuple = new (char letter, byte minCount, byte maxCount)[5];
        private (char letter, byte bad, byte wrong, byte good)[] trimList = new (char letter, byte bad, byte wrong, byte good)[5];
        byte[,] AlreadyForbidden = new byte[26, 5];
        byte[] AlreadyRequired = new byte[5];
        private char[] answers = new char[5];
        private uint intWord;
        private int[] intResult = new int[5];

        public static List<string> GetLines() => File.ReadAllLines("FiveLetterWords.txt").ToList();

        public static void GetAllocations(long startMemory, string data)
        {
            var endMemory = GC.GetAllocatedBytesForCurrentThread();
            var temp = (endMemory - startMemory) / 1024.0 / 1024.0;
            Trace.WriteLine($"{data}: {endMemory} - {startMemory}= {Math.Round(temp, 3)} mb.");
        }
        /////*GetAllocations(StartMemory, Log());*/
        public static string Log([CallerFilePath] string file = null, [CallerLineNumber] int line = 0) => $" {Path.GetFileName(file)}, {line}";

        public unsafe int[] TryAnswersRemove(int guessesRemaining1, IList<string> wordList, string wordToGuess, bool outPut, uint[] intWords)
        {
            string mostDiverseWord;
            //_startMemory = GC.GetAllocatedBytesForCurrentThread();
            Array.Clear(_diversityCharacters);
            Array.Clear(AlreadyForbidden);
            Array.Clear(AlreadyRequired);
            Array.Clear(intResult);
            byte* numbers = stackalloc byte[wordList.Count];
            while (guessesRemaining1 > 0 && intResult[0] + intResult[1] + intResult[2] + intResult[3] + intResult[4] != 15)
            {
                if (guessesRemaining1 < 6)
                {
                    //var sw = new Stopwatch();
                    //sw.Start();

                    var intCountFilter = new (byte letter, byte minCount, byte maxCount)[letterCountTuple.Length];
                    for (int i = 0; i < letterCountTuple.Length; i++) { intCountFilter[i] = ((byte)(letterCountTuple[i].letter - 'a'), letterCountTuple[i].minCount, letterCountTuple[i].maxCount); }
                    var mostDiverseWordIndex = PrunePossibleWords(wordList, letterCountTuple, goodLetterPositions, badLetterPositions, numbers, intWords, intCountFilter);

                    //Trace.WriteLine($"Guesses remaining: {guessesRemaining1}, target={wordToGuess}, prune time: {sw.Elapsed.TotalMilliseconds}");

                    mostDiverseWord = wordList[mostDiverseWordIndex];
                    numbers[mostDiverseWordIndex] = 1;
                    _maxDiversity = Math.Min(_maxDiversity, _runningDiversity);
                }
                else
                {
                    mostDiverseWord = wordList[41];
                    //wordList[41] = null;
                    numbers[41] = 1;
                }

                Array.Clear(letterCountTuple);
                Array.Clear(trimList);
                Array.Clear(goodLetterPositions);
                Array.Clear(badLetterPositions);

#if DEBUG
                if (outPut)
                {
                    var remainingWordCount = 0; for (int i = 0; i < wordList.Count; i++) { if (numbers[i] == 0) { remainingWordCount++; } }
                    Console.WriteLine($"RoboGuess: {mostDiverseWord} out of {remainingWordCount + 1} words.");
                }
#endif
                intResult = EvaluateResponse(mostDiverseWord, wordToGuess);
                if (intResult[0] + intResult[1] + intResult[2] + intResult[3] + intResult[4] == 0) { continue; }

                SetPruners(mostDiverseWord);

                guessesRemaining1--;
#if DEBUG
                //if (outPut) { Console.WriteLine(new string(_result.ToArray())); }
#endif
            }
            return intResult;
        }

        private void SetPruners(string mostDiverseWord)
        {
            for (var i = 0; i < mostDiverseWord.Length; i++) //Very small loop.
            {
                var index = mostDiverseWord[i] - 'a';
                letterResults[index].bad += (byte)(intResult[i] == 1 ? 1 : 0);
                letterResults[index].wrong += (byte)(intResult[i] == 2 ? 1 : 0);
                letterResults[index].good += (byte)(intResult[i] == 3 ? 1 : 0);

                if (intResult[i] == 2 && AlreadyForbidden[index, i] == 0)
                {
                    badLetterPositions[i] = mostDiverseWord[i];
                    AlreadyForbidden[index, i] = 1;
                }

                if (intResult[i] == 3 && AlreadyRequired[i] == 0)
                {
                    goodLetterPositions[i] = mostDiverseWord[i];
                    AlreadyRequired[i] = 1;
                }
            }

            var maxTempIndex = 0;
            for (var i = 0; i < letterResults.Length; i++)
            {
                if (letterResults[i].bad + letterResults[i].wrong + letterResults[i].good > 0)
                {
                    trimList[maxTempIndex] = ((char)('a' + i), letterResults[i].bad, letterResults[i].wrong, letterResults[i].good);
                    letterResults[i] = (0, 0, 0);
                    maxTempIndex++;
                }
            }

            var letterCountTupleCount = 0;
            for (var i = 0; i < maxTempIndex; i++)
            {
                var upperLimit = byte.MaxValue;
                if (trimList[i].bad > 0)
                {
                    /*Then we know the upper limit*/
                    upperLimit = (byte)(trimList[i].good + trimList[i].wrong);
                }

                byte lowerLimit = (byte)(trimList[i].good + trimList[i].wrong);
                letterCountTuple[letterCountTupleCount] = ((trimList[i].letter, lowerLimit, upperLimit));
                letterCountTupleCount++;
            }
        }

        public unsafe int PrunePossibleWords(IList<string> wordList,
            (char letter, byte minCount, byte maxCount)[] letterCountTuple,
            char[] goodLetterPositions,
            char[] badLetterPositions,
            byte* numbers, uint[] intWords, (byte letter, byte minCount, byte maxCount)[] intCountFilter)
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
                if (numbers[i] == 1) continue;
                var word = wordList[i];
                for (var index = 0; index < 5; index++)
                {
                    if (badLetterPositions[index] != '\0')
                    {
                        if (word[index] == badLetterPositions[index])
                        {
                            word = null;
                            //wordList[i] = null;
                            numbers[i] = 1;
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
                            //wordList[i] = null;
                            numbers[i] = 1;
#if DEBUG
                            wordsDeltedByPosition++;
#endif
                            break;
                        }
                    }
                }

                if (numbers[i] == 1) continue;

                //intWords

                intWord = intWords[i];

                foreach (var tuple in intCountFilter)
                {
                    count = 0;
                    if ((0b11111 & (intWord >> 0 * 5)) == tuple.letter) count++;
                    if ((0b11111 & (intWord >> 1 * 5)) == tuple.letter) count++;
                    if ((0b11111 & (intWord >> 2 * 5)) == tuple.letter) count++;
                    if ((0b11111 & (intWord >> 3 * 5)) == tuple.letter) count++;
                    if ((0b11111 & (intWord >> 4 * 5)) == tuple.letter) count++;

                    if (count > tuple.maxCount || count < tuple.minCount)
                    {
                        numbers[i] = 1;
                        break;
                    }
                }

                //Get most varied word
                if (numbers[i] == 1) continue;
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

        public int[] EvaluateResponse(string guessLetters, string targetWord)
        {
            intResult = new int[5]; //0 = blank, 1 = X, 2 = Y, 3 = G
            Array.Clear(intResult);

            //if (guessLetters.Length != 5) return "     ";
            answers = targetWord.ToArray();

            for (var i = 0; i < 5; i++)
            {
                if (guessLetters[i] == targetWord[i])
                {
                    intResult[i] = 3;
                    answers[i] = ' ';
                }
            }

            for (var i = 0; i < 5; i++)
            {

                if (intResult[i] != 0) { continue; }

                var index = -1;

                for (var index1 = 0; index1 < 5; index1++)
                {
                    if (answers[index1] == guessLetters[i])
                    {
                        index = index1;
                        break;
                    }
                }

                if (index == -1)
                {
                    intResult[i] = 1;
                    continue;
                }

                intResult[i] = 2;
                answers[index] = ' ';
            }
            return intResult;
        }

        public static uint StringToInt(string ss)
        {
            uint l = 0;
            for (var i = 0; i < 5; i++)
            {
                l |= (byte)(ss[i] - 'a');
                l <<= 5;
            }
            return l >> 5;
        }
    }

}