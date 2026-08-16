using PuffinProof.Core;

namespace PuffinProof.Tests;

public class FieldTextTests
{
    [Fact]
    public void Detects_word_before_space()
    {
        var word = FieldText.CompletedWordBeforeCaret("teh ", 4);
        Assert.NotNull(word);
        Assert.Equal("teh", word!.Word);
        Assert.Equal(" ", word.Trailing);
        Assert.Equal(0, word.Start);
        Assert.Equal(4, word.End);
    }

    [Fact]
    public void Detects_word_before_period()
    {
        var word = FieldText.CompletedWordBeforeCaret("recieve.", 8);
        Assert.Equal("recieve", word?.Word);
        Assert.Equal(".", word?.Trailing);
    }

    [Fact]
    public void Ignores_mid_word_caret()
    {
        Assert.Null(FieldText.CompletedWordBeforeCaret("teh", 3));
    }

    [Fact]
    public void Replaces_only_the_flagged_span()
    {
        var word = FieldText.CompletedWordBeforeCaret("say teh ", 8);
        Assert.NotNull(word);
        Assert.Equal("say the ", FieldText.Replace("say teh ", word!, "the"));
    }

    [Fact]
    public void Skips_enter()
    {
        Assert.Null(FieldText.CompletedWordBeforeCaret("teh\r", 4));
    }

    [Fact]
    public void Apply_keeps_text_typed_after_the_popup()
    {
        var flagged = FieldText.CompletedWordBeforeCaret("say teh ", 8);
        Assert.NotNull(flagged);
        Assert.Equal("say the more stuff ", FieldText.Replace("say teh more stuff ", flagged!, "the"));
    }

    [Fact]
    public void Apply_finds_the_word_if_it_shifted()
    {
        var flagged = FieldText.CompletedWordBeforeCaret("teh ", 4);
        Assert.NotNull(flagged);
        Assert.Equal("note the ", FieldText.Replace("note teh ", flagged!, "the"));
    }

    [Fact]
    public void Apply_does_nothing_if_the_word_is_gone()
    {
        var flagged = FieldText.CompletedWordBeforeCaret("teh ", 4);
        Assert.NotNull(flagged);
        Assert.Equal("already fixed ", FieldText.Replace("already fixed ", flagged!, "the"));
    }
}
