using JustSpell.Core;

namespace JustSpell.Tests;

public class WordTrackerTests
{
    [Fact]
    public void Completes_word_on_space()
    {
        var tracker = new WordTracker();
        CompletedWord? seen = null;
        tracker.WordCompleted += w => seen = w;

        Type(tracker, "teh");
        var completed = tracker.Handle(CharKey(' '));

        Assert.NotNull(completed);
        Assert.Equal("teh", completed!.Word);
        Assert.Equal(" ", completed.Trailing);
        Assert.Equal("teh", seen?.Word);
        Assert.Equal(string.Empty, tracker.CurrentWord);
    }

    [Fact]
    public void Keeps_contractions_together()
    {
        var tracker = new WordTracker();
        Type(tracker, "don't");
        var completed = tracker.Handle(CharKey(' '));
        Assert.Equal("don't", completed?.Word);
    }

    [Fact]
    public void Backspace_edits_the_buffer()
    {
        var tracker = new WordTracker();
        Type(tracker, "thex");
        tracker.Handle(new TypedKey(8, null, false, false, false, false, false));
        var completed = tracker.Handle(CharKey(' '));
        Assert.Equal("the", completed?.Word);
    }

    [Fact]
    public void Ctrl_combo_resets_the_buffer()
    {
        var tracker = new WordTracker();
        Type(tracker, "teh");
        tracker.Handle(new TypedKey('A', "a", Ctrl: true, false, false, false, false));
        Assert.Equal(string.Empty, tracker.CurrentWord);
        Assert.Null(tracker.Handle(CharKey(' ')));
    }

    [Fact]
    public void Ignores_injected_keys()
    {
        var tracker = new WordTracker();
        tracker.Handle(new TypedKey('A', "a", false, false, false, false, IsInjected: true));
        Assert.Equal(string.Empty, tracker.CurrentWord);
    }

    [Fact]
    public void Completes_on_punctuation()
    {
        var tracker = new WordTracker();
        Type(tracker, "recieve");
        var completed = tracker.Handle(CharKey('.'));
        Assert.Equal("recieve", completed?.Word);
        Assert.Equal(".", completed?.Trailing);
        Assert.Equal(8, completed?.TypedLength);
    }

    private static void Type(WordTracker tracker, string text)
    {
        foreach (var ch in text)
        {
            tracker.Handle(CharKey(ch));
        }
    }

    private static TypedKey CharKey(char ch)
    {
        // Use real Win32 virtual keys for letters/space; a dummy code for punctuation
        // so ASCII 39/46 are not mistaken for Right/Delete.
        var vk = ch switch
        {
            >= 'a' and <= 'z' => char.ToUpperInvariant(ch),
            >= 'A' and <= 'Z' => ch,
            ' ' => 0x20,
            '\b' => 8,
            '\t' => 9,
            '\r' => 13,
            _ => 0xE8
        };
        return new TypedKey(vk, ch.ToString(), false, false, false, false, false);
    }
}
