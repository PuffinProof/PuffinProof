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

    [Theory]
    [InlineData("the", true)]
    [InlineData("well-known", true)]
    [InlineData("don't", true)]
    [InlineData("a<script>", false)]
    [InlineData("C:\\Windows", false)]
    public void Replacements_are_sanitized(string word, bool ok)
    {
        Assert.Equal(ok, WordRules.IsSafeReplacement(word));
    }

    [Fact]
    public void Long_replacements_are_rejected()
    {
        Assert.False(WordRules.IsSafeReplacement(new string('x', 65)));
    }

    [Theory]
    [InlineData("en_US", true)]
    [InlineData("fr", true)]
    [InlineData("..\\evil", false)]
    [InlineData("en/US", false)]
    public void Language_ids_are_constrained(string id, bool ok)
    {
        Assert.Equal(ok, WordRules.IsSafeLanguageId(id));
    }

    [Fact]
    public void Enter_is_a_commit_delimiter()
    {
        Assert.True(WordRules.IsCommitDelimiter("\r"));
        Assert.False(WordRules.IsCommitDelimiter(" "));
    }
}
