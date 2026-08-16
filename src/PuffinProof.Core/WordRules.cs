using System.Text.RegularExpressions;

namespace PuffinProof.Core;

public static class WordRules
{
    private static readonly Regex EmailLike = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UrlLike = new(
        @"^(https?://|www\.)|(\.[a-z]{2,6})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool IsWordCharacter(char c, bool wordHasContent)
    {
        if (char.IsLetter(c))
        {
            return true;
        }

        // Digits stay in the token so we can skip the whole thing later.
        if (char.IsDigit(c))
        {
            return true;
        }

        // Contractions and hyphenates: don't, well-known.
        return wordHasContent && (c is '\'' or '\u2019' or '-');
    }

    public static bool IsDelimiter(char c) =>
        char.IsWhiteSpace(c) || (char.IsPunctuation(c) && c is not ('\'' or '\u2019' or '-')) || char.IsSymbol(c);

    public static bool ShouldSkipCheck(string word, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return true;
        }

        if (word.Length < settings.MinWordLength)
        {
            return true;
        }

        if (!word.Any(char.IsLetter))
        {
            return true;
        }

        if (settings.IgnoreWordsWithDigits && word.Any(char.IsDigit))
        {
            return true;
        }

        if (word.Contains('_'))
        {
            return true;
        }

        if (word.StartsWith('@') || word.StartsWith('#') || word.StartsWith('$'))
        {
            return true;
        }

        if (EmailLike.IsMatch(word))
        {
            return true;
        }

        if (word.Contains('.') && UrlLike.IsMatch(word))
        {
            return true;
        }

        if (word.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
            word.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool IsCommitDelimiter(string trailing) =>
        trailing is "\r" or "\n" or "\r\n" or "\t";

    public static string ApplyCapitalization(string original, string suggestion)
    {
        if (string.IsNullOrEmpty(suggestion) || string.IsNullOrEmpty(original))
        {
            return suggestion;
        }

        var originalLetters = original.Where(char.IsLetter).ToArray();
        if (originalLetters.Length == 0)
        {
            return suggestion;
        }

        if (originalLetters.All(char.IsUpper))
        {
            return suggestion.ToUpperInvariant();
        }

        if (char.IsUpper(originalLetters[0]) && originalLetters.Skip(1).All(char.IsLower))
        {
            return char.ToUpperInvariant(suggestion[0]) + suggestion[1..];
        }

        return suggestion;
    }
}
