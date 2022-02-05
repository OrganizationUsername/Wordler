using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wordler.Core
{
    public struct Solver
    {
        private long _startMemory;
        private int _maxDiversity = 5;
        private readonly int[] _diversityCharacters = new int[26];
        private int _runningDiversity;
        private int _currentDiversity;
        private int _winningIndex;
        readonly (byte bad, byte wrong, byte good)[] letterResults = new (byte, byte, byte)[26];//Get a unique list of letters
        public byte[] goodBytePositions = new byte[5];
        public byte[] badBytePositions = new byte[5];
        readonly (byte letter, byte minCount, byte maxCount)[] intCountFilter = new (byte letter, byte minCount, byte maxCount)[5];
        private readonly (byte letter, byte bad, byte wrong, byte good)[] trimList = new (byte letter, byte bad, byte wrong, byte good)[5];
        readonly byte[,] AlreadyForbidden = new byte[26, 5];
        readonly byte[] AlreadyRequired = new byte[5];
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

        public unsafe int[] TryAnswersRemove(int guessesRemaining1, bool outPut, uint[] intWords, uint answerInt)
        {
            uint mostDiverseUint;
            //_startMemory = GC.GetAllocatedBytesForCurrentThread();
            Array.Clear(_diversityCharacters);
            Array.Clear(AlreadyForbidden);
            Array.Clear(AlreadyRequired);
            Array.Clear(intResult);

            byte* numbers = stackalloc byte[intWords.Length];
            while (guessesRemaining1 > 0 && intResult[0] + intResult[1] + intResult[2] + intResult[3] + intResult[4] != 15)
            {
                if (guessesRemaining1 < 6)
                {
                    //var sw = new Stopwatch();
                    //sw.Start();
                    var mostDiverseWordIndex = PrunePossibleWords(numbers, intWords, intCountFilter);

                    mostDiverseUint = intWords[mostDiverseWordIndex];
                    numbers[mostDiverseWordIndex] = 1;
                    _maxDiversity = Math.Min(_maxDiversity, _runningDiversity);
                }
                else
                {
                    mostDiverseUint = intWords[41];
                    numbers[41] = 1;
                }

                Array.Clear(intCountFilter);

                intResult = EvaluateResponse(answerInt, mostDiverseUint);
                if (intResult[0] + intResult[1] + intResult[2] + intResult[3] + intResult[4] == 0) { continue; }

                SetPruners(mostDiverseUint);

                guessesRemaining1--;
            }
            return intResult;
        }

        private void SetPruners(uint mostDiverseUint)
        {
            for (var i = 0; i < 5; i++) //Very small loop.
            {
                badBytePositions[i] = byte.MaxValue;
                goodBytePositions[i] = byte.MaxValue;
                var index = (byte)(0b11111 & (mostDiverseUint >> (20 - 5 * i)));
                letterResults[index].bad += (byte)(intResult[i] == 1 ? 1 : 0);
                letterResults[index].wrong += (byte)(intResult[i] == 2 ? 1 : 0);
                letterResults[index].good += (byte)(intResult[i] == 3 ? 1 : 0);

                if (intResult[i] == 2 && AlreadyForbidden[index, i] == 0)
                {
                    badBytePositions[i] = (byte)(0b11111 & (mostDiverseUint >> (20 - 5 * i)));
                    AlreadyForbidden[index, i] = 1;
                }

                if (intResult[i] == 3 && AlreadyRequired[i] == 0)
                {
                    goodBytePositions[i] = (byte)(0b11111 & (mostDiverseUint >> (20 - 5 * i)));
                    AlreadyRequired[i] = 1;
                }
            }

            var maxTempIndex = 0;
            for (var i = 0; i < letterResults.Length; i++)
            {
                if (letterResults[i].bad + letterResults[i].wrong + letterResults[i].good > 0)
                {
                    trimList[maxTempIndex] = ((byte)i, letterResults[i].bad, letterResults[i].wrong, letterResults[i].good);
                    letterResults[i] = (0, 0, 0); //reset for next time
                    maxTempIndex++;
                }
            }

            var letterCountTupleCount = 0;
            for (var i = 0; i < 5; i++)
            {
                var upperLimit = byte.MaxValue;
                if (trimList[i].bad > 0) { /*Then we know the upper limit*/ upperLimit = (byte)(trimList[i].good + trimList[i].wrong); }
                byte lowerLimit = (byte)(trimList[i].good + trimList[i].wrong);
                intCountFilter[letterCountTupleCount] = ((trimList[i].letter), lowerLimit, upperLimit);
                letterCountTupleCount++;
            }
        }

        public unsafe int PrunePossibleWords(
            byte* numbers, uint[] intWords,
            (byte letter, byte minCount, byte maxCount)[] intCountFilter)
        {
            int count;
            _currentDiversity = 0;
            _winningIndex = 0;
            _runningDiversity = 0;

            for (var i = 0; i < intWords.Length; i++)
            {
                if (numbers[i] == 1) continue;
                intWord = intWords[i];
                for (var index = 0; index < 5; index++)
                {
                    if (badBytePositions[index] != byte.MaxValue) //It was set, MaxValue is default since 'a'=0. :(
                    {
                        if ((byte)(0b11111 & (intWord >> (4 - index) * 5)) == badBytePositions[index])
                        {
                            numbers[i] = 1;
                            break;
                        }
                    }
                    if (goodBytePositions[index] != byte.MaxValue)
                    {
                        if ((byte)(0b11111 & (intWord >> (4 - index) * 5)) != goodBytePositions[index])
                        {
                            numbers[i] = 1;
                            break;
                        }
                    }
                }

                if (numbers[i] == 1) continue;

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
                for (var j = 0; j < 5; j++)
                {
                    _diversityCharacters[(0b11111 & (intWord >> (4 - j) * 5))]++;
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

        public int[] EvaluateResponse(uint answerInt, uint guessUint)
        {
            intResult = new int[5]; //0 = blank, 1 = X, 2 = Y, 3 = G
            Array.Clear(intResult);

            for (var i = 0; i < 5; i++)
            {
                if ((byte)(0b11111 & (answerInt >> (4 - i) * 5)) == (byte)(0b11111 & (guessUint >> (4 - i) * 5)))
                {
                    intResult[i] = 3;
                    answerInt |= (uint)(0b11111 << (4 - i) * 5);
                }
            }

            for (var i = 0; i < 5; i++)
            {
                if (intResult[i] != 0) { continue; }
                var index = -1;
                for (var index1 = 0; index1 < 5; index1++)
                {
                    if ((0b11111 & (answerInt >> (4 - index1) * 5)) == (0b11111 & (guessUint >> (4 - i) * 5)))
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
                answerInt |= (uint)(0b11111 << ((4 - index) * 5));
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

        public static string IntToString(uint ss)
        {
            uint l;
            char[] result = new char[5];
            for (var i = 0; i < 5; i++)
            {
                l = (char)((ss & 0b11111) + 'a');
                result[4 - i] = (char)l;
                ss >>= 5;
            }
            return new(result);
        }
    }

}