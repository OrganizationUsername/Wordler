using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Wordler.Core;

var outPut = false;

if (!File.Exists("FiveLetterWords.txt"))
{
    var lines = File.ReadAllLines("words.txt");//From: https://raw.githubusercontent.com/dwyl/english-words/master/words.txt
    Console.WriteLine($"{lines.Length} words.");
    File.WriteAllLines("FiveLetterWords.txt", lines.Where(x => x.Length == 5 && x.All(y => char.IsLetter(y))).Select(y => y.ToLower()).ToHashSet().ToList());
}

var oneTimeList = File.ReadAllLines("FiveLetterWords.txt").ToList();

Console.WriteLine($"Press 1 for human. Everything else interpreted as robot.");
var tempInput = Console.ReadLine();
var human = tempInput is null || tempInput.Contains('1');
Console.WriteLine(human ? "Playing as all human guesses." : "Playing as robot.");
Console.WriteLine($"Input number of words to solve.");
var numberString = Console.ReadLine();

var numberToTake = int.TryParse(numberString, out int parsedInt) ? parsedInt : 1;
var successes = 0;


for (var s = 0; s < numberToTake; s++)
{
    var guessesRemaining = 6;
    var ran = new Random(1);
    var possibles = oneTimeList.OrderBy(c => ran.NextDouble()).ToList();

    var randomIndex = new Random().Next(0, possibles.Count - 1);
    var answerWord = possibles[randomIndex];
    //answerWord = "poppy";
    Console.WriteLine(answerWord);
    var result = TryAnswers(guessesRemaining, human, possibles, answerWord);

    var success = result.All(x => x == 'G');
    if (success) { successes++; Console.WriteLine($"Good job."); }
    else { Console.WriteLine($"Failure."); }
    if (possibles.Count < 100) { Console.WriteLine($"{string.Join(", ", possibles)}"); }
}

Console.WriteLine($"{successes} successes out of {numberToTake}.");

List<char>? TryAnswers(int guessesRemaining1, bool b, List<string> list, string s)
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

            //Debugger.Break();
            foreach (var n in forbiddenLetters)
            {
                list = list.Where(p => p.Count(c => c == n.Key) <= n.Value).ToList();
            }

            for (var n = 0; n < forbiddenLetterPositions.Count; n++)
            {
                list = list.Where(p => !forbiddenLetterPositions[n].Contains(p[n])).ToList();
            }

            //ToDo: I should figure out how to prune letters more effectively, it might not be easy with multiple letters in a word.

            list = list.OrderByDescending(c => c.Distinct().ToHashSet().Count).ToList();
            guess = list.First().ToList();

            if (!b)
            {
                if (outPut)
                {
                    Console.WriteLine($"RoboGuess: {new string(guess.ToArray())} out of {list.Count} words.");
                }
            }

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

        if (outPut)
        {

            //Console.WriteLine("ForbiddenLetters: " + string.Join(", ", forbiddenLetters.Select(l => $"{l.Key}: { string.Join(", ", l.Value)}")));
            //Console.WriteLine("Letters: " + string.Join(", ", includedLetters.Select(l => $"{l.Key}: {l.Value}")));
            //Console.WriteLine("Position: " + string.Join(", ", knownPositions.Select(p => $"{p.Key}: {p.Value}")));

            Console.WriteLine(new string(result.ToArray()));
        }
    }

    return result;
}

