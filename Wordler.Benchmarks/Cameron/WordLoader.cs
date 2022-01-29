using System.Text;
using Wordler.Benchmarks;

namespace CameronAavik.Wordler;

public static class WordLoader
{
    private const string FullDictionaryUrl = "https://raw.githubusercontent.com/dwyl/english-words/master/words.txt";
    private const string DictionaryCache = "dictionary.txt";

    public static async Task<List<string>> LoadAsync()
    {
        if (!File.Exists(DictionaryCache))
            await DownloadWordsAndSaveToCacheAsync();

        return await ReadWordsFromDictionaryCacheAsync();
    }

    private static async Task DownloadWordsAndSaveToCacheAsync()
    {
        //using var httpClient = new HttpClient();
        //using var wordsStream = await httpClient.GetStreamAsync(FullDictionaryUrl);
        //using var reader = new StreamReader(wordsStream);

        //var wordSet = new HashSet<string>();
        //while (await reader.ReadLineAsync() is string line)
        //{
        //    if (IsValid5LetterWord(line))
        //        wordSet.Add(line.ToLowerInvariant());
        //}

        //var arr = wordSet.ToArray();
        var arr = ReservedList.AbelWords;
        Array.Sort(arr);

        using var fileStream = File.Open(DictionaryCache, FileMode.Create, FileAccess.Write);
        foreach (var word in arr)
            await fileStream.WriteAsync(Encoding.ASCII.GetBytes($"{word}\n"));

        static bool IsValid5LetterWord(string word)
        {
            if (word.Length != 5)
                return false;

            foreach (var c in word)
            {
                if (c is not (>= 'a' and <= 'z') and not (>= 'A' and <= 'Z'))
                    return false;
            }

            return true;
        }
    }

    private static async Task<List<string>> ReadWordsFromDictionaryCacheAsync()
    {
        var words = new List<string>();

        using var fileStream = File.Open(DictionaryCache, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream);
        while (await reader.ReadLineAsync() is string line)
            words.Add(line);

        return words;
    }
}