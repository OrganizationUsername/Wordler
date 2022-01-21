using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wordler.Core;

if (!File.Exists("FiveLetterWords.txt"))
{
    var lines = File.ReadAllLines("words.txt");//From: https://raw.githubusercontent.com/dwyl/english-words/master/words.txt
    Console.WriteLine($"{lines.Length} words.");
    File.WriteAllLines("FiveLetterWords.txt", lines.Where(x => x.Length == 5 && x.All(y => char.IsLetter(y))).Select(y => y.ToLower()).ToHashSet().ToList());
}

var oneTimeList = File.ReadAllLines("FiveLetterWords.txt").ToList();

//answerWord = "robot";



Console.WriteLine($"Press 1 for human. Everything else interpreted as robot.");
var tempInput = Console.ReadLine();
var human = tempInput is null || tempInput.Contains('1');
Console.WriteLine(human ? "Playing as all human guesses." : "Playing as robot.");

Console.WriteLine($"Input number of words to solve.");
var numberString = Console.ReadLine();

var numberToTake = int.TryParse(numberString, out int parsedInt) ? parsedInt : 1;
var successes = 0;


foreach (var VARIABLE in oneTimeList.Take(numberToTake))
{
    var forbiddenLetters = new Dictionary<int, List<char>>();
    foreach (var i in Enumerable.Range(0, 5)) { forbiddenLetters.Add(i, new()); }

    var includedLetters = new Dictionary<char, int>();
    for (var c = 'a'; c <= 'z'; c++) { includedLetters.Add(c, 0); }
    var knownPositions = new Dictionary<int, char>();

    var guessesRemaining = 6;
    var possibles = oneTimeList.ToList();

    var randomIndex = new Random().Next(0, possibles.Count - 1);
    var answerWord = possibles[randomIndex];
    Console.WriteLine(answerWord);
    var result = TryAnswers(guessesRemaining, human, includedLetters, possibles, knownPositions, forbiddenLetters, answerWord);

    var success = result.All(x => x == 'G');
    if (success) { successes++; Console.WriteLine($"Good job."); }
    else { Console.WriteLine($"Failure."); }
    if (possibles.Count < 100) { Console.WriteLine($"{string.Join(", ", possibles)}"); }
}

Console.WriteLine($"{successes} successes out of {numberToTake}.");



List<char>? TryAnswers(int guessesRemaining1, bool b, Dictionary<char, int> dictionary, List<string> list, Dictionary<int, char> knownPositions1, Dictionary<int, List<char>> forbiddenLetters1, string s)
{
    var result1 = new List<char>() { ' ', ' ', ' ', ' ', ' ' };
    List<char> guess;
    while (guessesRemaining1 > 0 && (result1 is null || result1.Any(x => x != 'G')))
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

            foreach (var n in knownPositions1)
            {
                list = list.Where(p => p[n.Key] == n.Value).ToList();
            }

            for (var n = 0; n < forbiddenLetters1.Count; n++)
            {
                list = list.Where(p => !forbiddenLetters1[n].Contains(p[n])).ToList();
            }

            //ToDo: I should figure out how to prune letters more effectively, it might not be easy with multiple letters in a word.


            var index = 0;
            if (guessesRemaining1 > 5)
            {
                var tempList = list.Where(c => c.Distinct().ToHashSet().Count == 5).ToList();
                if (!tempList.Any()) tempList = list;
                var thisIndex = new Random(1).Next(0, tempList.Count - 1);
                index = list.FindIndex(p => p == tempList[thisIndex]);
                guess = tempList[thisIndex].ToList();
            }
            else
            {
                index = new Random(1).Next(0, list.Count - 1);
                guess = list[index].ToList();
            }

            if (!b)
            {
                Console.WriteLine($"RoboGuess: {new string(guess.ToArray())} out of {list.Count} words.");
            }

            list.RemoveAt(index);
        }

        result1 = Solver.EvaluateResponse(guess, s);
        if (result1 is null || result1.All(c => c == ' '))
        {
            continue;
        }

        for (var i = 0; i < result1.Count; i++)
        {
            if (result1[i] == 'G')
            {
                knownPositions1[i] = guess[i];
            }
            else
            {
                forbiddenLetters1[i].Add(guess[i]);
            }
        }

        var tempDictionary = new Dictionary<char, int>();
        for (var i = 0; i < result1.Count; i++)
        {
            if (result1[i] == 'Y' || result1[i] == 'G')
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

            if (result1[i] == 'G')
            {
                knownPositions1.TryAdd(i, guess[i]);
            }
        }


        guessesRemaining1--;
        //Console.WriteLine("ForbiddenLetters: " + string.Join(", ", forbiddenLetters.Select(l => $"{l.Key}: { string.Join(", ", l.Value)}")));
        //Console.WriteLine("Letters: " + string.Join(", ", includedLetters.Select(l => $"{l.Key}: {l.Value}")));
        //Console.WriteLine("Position: " + string.Join(", ", knownPositions.Select(p => $"{p.Key}: {p.Value}")));
        Console.WriteLine(new string(result1.ToArray()));
    }

    return result1;
}

