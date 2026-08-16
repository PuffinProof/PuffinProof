using PuffinProof.Core;

namespace PuffinProof.Tests;

public class WordRulesTests
{
    private readonly AppSettings _settings = new();

    [Theory]
    [InlineData("teh", false)]
    [InlineData("a", true)]
    [InlineData("Windows11", true)]
    [InlineData("user_id", true)]
    [InlineData("hello@example.com", true)]
    [InlineData("https://example.com", true)]
    [InlineData("www.example.com", true)]
    [InlineData("12345", true)]
    [InlineData("", true)]
    public void Skip_rules(string word, bool skip)
    {
        Assert.Equal(skip, WordRules.ShouldSkipCheck(word, _settings));
    }

    [Fact]
    public void Digits_are_checked_when_setting_is_off()
    {
        var settings = new AppSettings { IgnoreWordsWithDigits = false };
        Assert.False(WordRules.ShouldSkipCheck("Windows11", settings));
    }

    [Theory]
    [InlineData("Teh", "the", "The")]
    [InlineData("TEH", "the", "THE")]
    [InlineData("teh", "the", "the")]
    public void Capitalization_follows_the_typed_word(string original, string suggestion, string expected)
    {
        Assert.Equal(expected, WordRules.ApplyCapitalization(original, suggestion));
    }

    [Fact]
    public void Enter_is_a_commit_delimiter()
    {
        Assert.True(WordRules.IsCommitDelimiter("\r"));
        Assert.False(WordRules.IsCommitDelimiter(" "));
    }
}
