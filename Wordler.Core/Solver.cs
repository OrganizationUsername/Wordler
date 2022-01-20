namespace Wordler.Core
{
    public static class Solver
    {
        public static List<char> EvaluateResponse(List<char> guessLetters, string targetWord)
        {
            var result = new List<char>() { ' ', ' ', ' ', ' ', ' ' };
            if (guessLetters.Count != 5) return null;
            var answers = targetWord.ToList();

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
                var index = answers.FindIndex(x => x == guessLetters[i]);
                if (index == -1)
                {
                    result[i] = 'X';
                    continue;
                }
                result[i] = 'Y';
                answers[index] = ' ';
            }

            return result;
        }
    }
}