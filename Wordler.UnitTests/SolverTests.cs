using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wordler.Core;
using Xunit;

namespace Wordler.UnitTests;

public class SolverTests
{
    public List<string> WordsList;
    private readonly Solver _solver = new();
    public SolverTests()
    {
        WordsList = Solver.GetLines();
    }

    [Fact]
    public void Test10Words_OK()
    {
        var count = 10;

        var ran = new Random(2);
        var randomWords = WordsList.OrderBy(l => ran.NextDouble()).Take(count).Select(c => c.ToCharArray()).ToList();
        var sb = new StringBuilder();
        var str = "";
        for (var index = 0; index < randomWords.Count; index++)
        {
            char[] s = randomWords[index];
            var reloadableWords = WordsList.Select(c => c.ToCharArray()).ToList();
            Solver solver = new Solver();
            sb.Append(new string(solver.TryAnswersRemove(6, reloadableWords, s, false).ToArray()));
        }

        Assert.Equal("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG", sb.ToString());
    }

    [Fact]
    public void Test100Words_Bad()
    {
        var count = 100;

        var ran = new Random(2);
        var randomWords = WordsList.OrderBy(l => ran.NextDouble()).Take(count).Select(c => c.ToCharArray()).ToList();
        var sb = new StringBuilder();
        var str = "";
        for (var index = 0; index < randomWords.Count; index++)
        {
            var s = randomWords[index];
            var reloadableWords = WordsList.Select(c => c.ToCharArray()).ToList();
            Solver solver = new Solver();
            sb.Append(new string(solver.TryAnswersRemove(6, reloadableWords, s, false).ToArray()));
        }

        Assert.Contains(sb.ToString(), s => s != 'G');
    }

    [Fact]
    public void ReadFiveLetterWordFile()
    {
        Assert.Equal(21952, WordsList.Count);
    }

    [Fact]
    public void EvaluatePerfectGuess_OK()
    {
        var desiredWord = "asdfa".ToCharArray();
        var guessWord = "asdfa".ToCharArray();
        var result = _solver.EvaluateResponse(guessWord, desiredWord);

        Assert.Equal("GGGGG", new(result.ToArray()));
    }

    [Fact]
    public void EvaluateBadGuess_OK()
    {
        var desiredWord = "asdfa".ToCharArray();
        var guessWord = "asdfx".ToCharArray();
        var result = _solver.EvaluateResponse(guessWord, desiredWord);

        Assert.NotEqual("GGGGG", new(result.ToArray()));
    }

    [Fact]
    public void PruneImpossibleWords_RequiredLetters_OK()
    {
        var wordList = new List<char[]>() { "robot".ToCharArray(), "sabot".ToCharArray(), "dabot".ToCharArray() };

        var forbiddenLetters = new Dictionary<char, int> { { 'r', 1 } };

        Solver solver = new Solver();
        solver.PrunePossibleWords(
            wordList,
            forbiddenLetters,
            Array.Empty<char>(),
            Array.Empty<int>(),
            Array.Empty<List<char>>());
        Assert.Equal(1, wordList.Count(x => x is not null));
        Assert.Equal("robot", wordList.First(x => x is not null));
    }

    [Fact]
    public void PruneImpossibleWords_KnownPositions_OK()
    {
        var wordList = new List<char[]>() { "robot".ToCharArray(), "sabot".ToCharArray(), "darot".ToCharArray() };

        var knownPositions = new char[] { 'r', default, default, default, default };

        Solver solver = new Solver();
        solver.PrunePossibleWords(
            wordList,
            new(),
            knownPositions,
            Array.Empty<int>(),
            Array.Empty<List<char>>());
        Assert.Equal(1, wordList.Count(x => x is not null));
        Assert.Equal("robot", wordList.First(x => x is not null));
    }

    [Fact]
    public void PruneImpossibleWords_ForbiddenLetters_OK()
    {
        var wordList = new List<char[]>() { "robot".ToCharArray(), "sabot".ToCharArray(), "dabot".ToCharArray() };

        var forbiddenLetters = new int[] { 0 };

        Solver solver = new Solver();
        solver.PrunePossibleWords(
            wordList,
            new(),
            Array.Empty<char>(),
            forbiddenLetters,
            Array.Empty<List<char>>());
        Assert.Equal(1, wordList.Count(x => x is not null));
        Assert.Equal("robot", wordList.First(x => x is not null));
    }

    [Fact]
    public void PruneImpossibleWords_ForbiddenLetterPosition_OK()
    {
        var wordList = new List<char[]>() { "robot".ToCharArray(), "sabot".ToCharArray(), "radio".ToCharArray() };

        var forbiddenLetterPositions = new List<char>[] { new(), new() { 'a' }, new(), new(), new() };

        Solver solver = new Solver();
        solver.PrunePossibleWords(
            wordList,
            new(),
            Array.Empty<char>(),
            Array.Empty<int>(),
            forbiddenLetterPositions);
        Assert.Equal(1, wordList.Count(x => x is not null));
        Assert.Equal("robot", wordList.First(x => x is not null));
    }

}