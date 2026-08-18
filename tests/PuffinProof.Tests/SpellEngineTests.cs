using PuffinProof.Core;

namespace PuffinProof.Tests;

public class SpellEngineTests
{
    private static SpellEngine CreateEngine()
    {
        var tempUser = Path.Combine(Path.GetTempPath(), "puffinproof-tests-" + Guid.NewGuid().ToString("n") + ".txt");
        var dictionaries = Path.Combine(AppContext.BaseDirectory, "Dictionaries");
        return SpellEngine.Load(dictionaries, "en_US", new UserDictionary(tempUser));
    }

    [Fact]
    public void Flags_common_misspellings()
    {
        var engine = CreateEngine();
        Assert.Equal(SpellDecision.Misspelled, engine.Evaluate("teh", new AppSettings()));
        Assert.Equal(SpellDecision.Misspelled, engine.Evaluate("recieve", new AppSettings()));
    }

    [Fact]
    public void Accepts_correct_words()
    {
        var engine = CreateEngine();
        Assert.Equal(SpellDecision.Correct, engine.Evaluate("the", new AppSettings()));
        Assert.Equal(SpellDecision.Correct, engine.Evaluate("receive", new AppSettings()));
        Assert.Equal(SpellDecision.Correct, engine.Evaluate("don't", new AppSettings()));
    }

    [Fact]
    public void Suggests_receive_for_recieve()
    {
        var engine = CreateEngine();
        var suggestions = engine.Suggest("recieve");
        Assert.Contains("receive", suggestions, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void User_dictionary_suppresses_the_flag()
    {
        var engine = CreateEngine();
        engine.AddToUserDictionary("PuffinProof");
        Assert.True(engine.IsCorrect("PuffinProof"));
        Assert.Equal(SpellDecision.Correct, engine.Evaluate("PuffinProof", new AppSettings()));
    }

    [Fact]
    public void Session_ignore_suppresses_the_flag()
    {
        var engine = CreateEngine();
        engine.IgnoreForSession("teh");
        Assert.Equal(SpellDecision.Correct, engine.Evaluate("teh", new AppSettings()));
    }

    [Fact]
    public void Extra_word_list_is_loaded()
    {
        var engine = CreateEngine();
        Assert.True(engine.IsCorrect("webhook") || engine.Evaluate("webhook", new AppSettings()) is SpellDecision.Correct or SpellDecision.Skip);
    }
}
