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
var possibles = File.ReadAllLines("FiveLetterWords.txt").ToList();
var randomIndex = new Random().Next(0, possibles.Count - 1);
var answerWord = possibles[randomIndex];
//answerWord = "robot";
Console.WriteLine(answerWord);

var forbiddenLetters = new Dictionary<int, List<char>>();
foreach (var i in Enumerable.Range(0, 5)) { forbiddenLetters.Add(i, new()); }

var includedLetters = new Dictionary<char, int>();
for (var c = 'a'; c <= 'z'; c++) { includedLetters.Add(c, 0); }
var knownPositions = new Dictionary<int, char>();

Console.WriteLine($"Press 1 for human.");
var tempInput = Console.ReadLine();
var human = tempInput is null || tempInput.Contains('1');
Console.WriteLine(human ? "Playing as all human guesses." : "Playing as robot.");

var guessesRemaining = 6;
List<char> guess;
var result = new List<char>() { ' ', ' ', ' ', ' ', ' ' };

while (guessesRemaining > 0 && (result is null || result.Any(x => x != 'G')))
{
    if (human)
    {
        guess = Console.ReadLine()?.ToList() ?? new List<char>();
    }
    else
    {
        var necessaryLetters = includedLetters.Where(l => l.Value > 0).Select(l => l.Key).ToList();

        foreach (var n in necessaryLetters) { possibles = possibles.Where(p => p.Contains(n)).ToList(); }

        foreach (var n in knownPositions) { possibles = possibles.Where(p => p[n.Key] == n.Value).ToList(); }

        for (var n = 0; n < forbiddenLetters.Count; n++) { possibles = possibles.Where(p => !forbiddenLetters[n].Contains(p[n])).ToList(); }

        //ToDo: I should figure out how to prune letters more effectively, it might not be easy with multiple letters in a word.


        var index = 0;
        if (guessesRemaining > 5)
        {
            var tempList = possibles.Where(c => c.Distinct().ToHashSet().Count == 5).ToList();
            if (!tempList.Any()) tempList = possibles;
            var thisIndex = new Random(1).Next(0, tempList.Count - 1);
            index = possibles.FindIndex(p => p == tempList[thisIndex]);
            guess = tempList[thisIndex].ToList();
        }
        else
        {
            index = new Random(1).Next(0, possibles.Count - 1);
            guess = possibles[index].ToList();
        }

        if (!human) { Console.WriteLine($"RoboGuess: {new string(guess.ToArray())} out of {possibles.Count} words."); }
        possibles.RemoveAt(index);
    }

    result = Solver.EvaluateResponse(guess, answerWord);
    if (result is null || result.All(c => c == ' ')) { continue; }

    for (var i = 0; i < result.Count; i++)
    {
        if (result[i] == 'G')
        {
            knownPositions[i] = guess[i];
        }
        else
        {
            forbiddenLetters[i].Add(guess[i]);
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
            includedLetters[kvp.Key] = Math.Max(includedLetters[kvp.Key], kvp.Value);
        }

        if (result[i] == 'G') { knownPositions.TryAdd(i, guess[i]); }
    }


    guessesRemaining--;
    //Console.WriteLine("ForbiddenLetters: " + string.Join(", ", forbiddenLetters.Select(l => $"{l.Key}: { string.Join(", ", l.Value)}")));
    //Console.WriteLine("Letters: " + string.Join(", ", includedLetters.Select(l => $"{l.Key}: {l.Value}")));
    //Console.WriteLine("Position: " + string.Join(", ", knownPositions.Select(p => $"{p.Key}: {p.Value}")));
    Console.WriteLine(new string(result.ToArray()));
}
result = new List<char>() { ' ', ' ', ' ', ' ', ' ' };


Console.WriteLine(result.All(x => x == 'G') ? "Good job." : "Failure.");
if (possibles.Count < 100) { Console.WriteLine($"{string.Join(", ", possibles)}"); }

