using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Wordler.Core;
/*
ToDo: Make sure I'm doing all of this:
- create a Set<> per letter (or an index per letter per position)
- pick a word
- exclude all Set<> for each discarded letter
- exclude all Set<> for each yellow letter on THAT position
- exclude all "per letter set" that don't have same green match
- find all set from "untried letter" that contains all match Set or yellow letter
- find intersection
- use the word with the most unused letter but where each letter match possible condition abode

idea: maximize "un-used letter" that still have match on yellow/green letter
 */

var outPut = true;
long startMemory = GC.GetAllocatedBytesForCurrentThread();
if (!File.Exists("FiveLetterWords.txt"))
{
    var lines = File.ReadAllLines("words.txt");//From: https://raw.githubusercontent.com/dwyl/english-words/master/words.txt
    Console.WriteLine($"{lines.Length} words.");
    File.WriteAllLines("FiveLetterWords.txt", lines.Where(x => x.Length == 5 && x.All(y => char.IsLetter(y))).Select(y => y.ToLower()).ToHashSet().ToList());
}

var oneTimeList = File.ReadAllLines("FiveLetterWords.txt").ToList();
var permanentList = oneTimeList.ToList();

var human = true;
Console.WriteLine($"Input number of words to solve. -1 for all words.");
//var numberString = Console.ReadLine();

var numberToTake = 10;// int.TryParse(numberString, out int parsedInt) ? parsedInt : 1;
//if (numberString == "-1") numberToTake = oneTimeList.Count - 1;
var successes = 0;

var sw = new Stopwatch();
sw.Start();

var rand = new Random(1);

var possibles = oneTimeList.ToList();
Solver.GetAllocations(startMemory, "Before  Loop: " + Solver.Log());

startMemory = GC.GetAllocatedBytesForCurrentThread();
Solver solver = new Solver();
for (var s = 0; s < numberToTake; s++)
{
    var guessesRemaining = 6;

    possibles.Clear();
    possibles.AddRange(permanentList); // 520 bytes allocated

    var randomIndex = rand.Next(0, possibles.Count - 1);
    var answerWord = possibles[randomIndex];
    oneTimeList.Remove(answerWord);
    //answerWord = "doggo";
    if (outPut)
    {
        Console.WriteLine(answerWord);
    }

    var result = solver.TryAnswersRemove(guessesRemaining, possibles, answerWord, outPut); // 21_280 bytes allocated

    //Solver.GetAllocations(startMemory, "After  Guess: " + Solver.Log());
    var success = result.All(x => x == 'G');
    if (success) { successes++; }
    if (outPut)
    {
        if (success) { Console.WriteLine($"Good job."); }
        else { Console.WriteLine($"Failure."); }
        if (possibles.Count(c => string.IsNullOrEmpty(c)) < 100) { Console.WriteLine($"{string.Join(", ", possibles.Where(c => c is not null))}"); }
    }
}

Solver.GetAllocations(startMemory, "Finished: " + Solver.Log());
Console.WriteLine($"{successes} successes out of {numberToTake} in {sw.ElapsedMilliseconds} ms.");