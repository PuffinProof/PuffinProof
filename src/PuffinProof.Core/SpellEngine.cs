using WeCantSpell.Hunspell;

namespace PuffinProof.Core;

public sealed class SpellEngine
{
    private readonly WordList _dictionary;
    private readonly UserDictionary _user;
    private readonly HashSet<string> _sessionIgnore = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _extras = new(StringComparer.OrdinalIgnoreCase);

    public SpellEngine(WordList dictionary, UserDictionary user, IEnumerable<string>? extras = null)
    {
        _dictionary = dictionary;
        _user = user;
        if (extras is not null)
        {
            foreach (var extra in extras)
            {
                var word = extra.Trim();
                if (word.Length > 0 && !word.StartsWith('#'))
                {
                    _extras.Add(word);
                }
            }
        }
    }

    public UserDictionary User => _user;

    public static SpellEngine Load(string dictionariesDirectory, string language, UserDictionary user)
    {
        var dic = Path.Combine(dictionariesDirectory, language + ".dic");
        var aff = Path.Combine(dictionariesDirectory, language + ".aff");
        if (!File.Exists(dic) || !File.Exists(aff))
        {
            throw new FileNotFoundException(
                $"Hunspell dictionary '{language}' was not found in '{dictionariesDirectory}'.");
        }

        var extrasPath = Path.Combine(dictionariesDirectory, "extra-en.txt");
        IEnumerable<string>? extras = null;
        if (File.Exists(extrasPath))
        {
            extras = File.ReadAllLines(extrasPath);
        }

        var wordList = WordList.CreateFromFiles(dic, aff);
        return new SpellEngine(wordList, user, extras);
    }

    public static IReadOnlyList<string> AvailableLanguages(string dictionariesDirectory)
    {
        if (!Directory.Exists(dictionariesDirectory))
        {
            return [];
        }

        return Directory.GetFiles(dictionariesDirectory, "*.dic")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool IsCorrect(string word)
    {
        if (_user.Contains(word) || _sessionIgnore.Contains(word) || _extras.Contains(word))
        {
            return true;
        }

        return _dictionary.Check(word);
    }

    public IReadOnlyList<string> Suggest(string word, int max = 5)
    {
        var raw = _dictionary.Suggest(word);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<string>(max);
        foreach (var suggestion in raw)
        {
            if (string.IsNullOrWhiteSpace(suggestion))
            {
                continue;
            }

            var adjusted = WordRules.ApplyCapitalization(word, suggestion);
            if (!seen.Add(adjusted))
            {
                continue;
            }

            results.Add(adjusted);
            if (results.Count >= max)
            {
                break;
            }
        }

        return results;
    }

    public SpellDecision Evaluate(string word, AppSettings settings)
    {
        if (WordRules.ShouldSkipCheck(word, settings))
        {
            return SpellDecision.Skip;
        }

        if (IsCorrect(word))
        {
            return SpellDecision.Correct;
        }

        return SpellDecision.Misspelled;
    }

    public void IgnoreForSession(string word) => _sessionIgnore.Add(word);

    public void AddToUserDictionary(string word) => _user.Add(word);
}

public enum SpellDecision
{
    Skip,
    Correct,
    Misspelled
}
