namespace PuffinProof.Core;

/// <summary>
/// Finds a just-finished word in a text field from the document text and caret.
/// Used when watching other apps via UI Automation instead of a keyboard hook.
/// </summary>
public static class FieldText
{
    public const int MaxReadLength = 32_768;

    public static FieldWord? CompletedWordBeforeCaret(string text, int caret)
    {
        if (string.IsNullOrEmpty(text) || caret < 1 || caret > text.Length)
        {
            return null;
        }

        var delimIndex = caret - 1;
        if (!WordRules.IsDelimiter(text[delimIndex]))
        {
            return null;
        }

        var trailing = text[delimIndex].ToString();
        if (WordRules.IsCommitDelimiter(trailing))
        {
            return null;
        }

        var end = delimIndex;
        var start = end;
        while (start > 0 && WordRules.IsWordCharacter(text[start - 1], start - 1 < end))
        {
            start--;
        }

        if (start >= end)
        {
            return null;
        }

        var word = text[start..end];
        return new FieldWord(word, trailing, start, caret);
    }

    public static string Replace(string text, FieldWord fieldWord, string replacement)
    {
        var span = LocateForReplace(text, fieldWord);
        if (span is null)
        {
            return text;
        }

        return text[..span.Start] + replacement + span.Trailing + text[span.End..];
    }

    /// <summary>
    /// Finds the same misspelled token in live text. Prefers the original
    /// character span if it is still that word; otherwise the closest copy.
    /// Returns null if the word is gone or the span would be unsafe.
    /// </summary>
    public static FieldWord? LocateForReplace(string liveText, FieldWord original)
    {
        if (string.IsNullOrEmpty(liveText) || string.IsNullOrEmpty(original.Word))
        {
            return null;
        }

        var token = original.Word + original.Trailing;
        if (token.Length == 0 || token.Length > liveText.Length)
        {
            return null;
        }

        if (SpanIsToken(liveText, original.Start, token))
        {
            return original with { End = original.Start + token.Length };
        }

        var best = -1;
        var bestDistance = int.MaxValue;
        for (var i = 0; i <= liveText.Length - token.Length; i++)
        {
            if (!SpanIsToken(liveText, i, token))
            {
                continue;
            }

            if (i > 0 && WordRules.IsWordCharacter(liveText[i - 1], true))
            {
                continue;
            }

            var distance = Math.Abs(i - original.Start);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        if (best < 0)
        {
            return null;
        }

        return original with { Start = best, End = best + token.Length };
    }

    private static bool SpanIsToken(string text, int start, string token) =>
        start >= 0 &&
        start + token.Length <= text.Length &&
        text.AsSpan(start, token.Length).SequenceEqual(token);

    public static string Clamp(string text) =>
        text.Length <= MaxReadLength ? text : text[..MaxReadLength];
}

public sealed record FieldWord(string Word, string Trailing, int Start, int End);
