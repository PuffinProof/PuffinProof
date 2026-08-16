namespace PuffinProof.Core;

public sealed class UserDictionary
{
    private readonly HashSet<string> _words = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path;

    public UserDictionary(string path)
    {
        _path = path;
        Reload();
    }

    public IReadOnlyCollection<string> Words => _words.OrderBy(static w => w, StringComparer.OrdinalIgnoreCase).ToArray();

    public int Count => _words.Count;

    public void Reload()
    {
        _words.Clear();
        if (!File.Exists(_path))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(_path))
        {
            var word = line.Trim();
            if (word.Length > 0 && !word.StartsWith('#'))
            {
                _words.Add(word);
            }
        }
    }

    public bool Contains(string word) => _words.Contains(word);

    public bool Add(string word)
    {
        word = word.Trim();
        if (word.Length == 0 || !_words.Add(word))
        {
            return false;
        }

        Persist();
        return true;
    }

    public bool Remove(string word)
    {
        if (!_words.Remove(word))
        {
            return false;
        }

        Persist();
        return true;
    }

    private void Persist()
    {
        var lines = _words
            .OrderBy(static w => w, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        File.WriteAllLines(_path, lines);
    }
}
