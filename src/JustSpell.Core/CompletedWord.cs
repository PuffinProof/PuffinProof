namespace JustSpell.Core;

public sealed record CompletedWord(string Word, string Trailing)
{
    public int TypedLength => Word.Length + Trailing.Length;
}
