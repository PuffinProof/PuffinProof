using System.Text;

namespace PuffinProof.Core;

public sealed class WordTracker
{
    private readonly StringBuilder _word = new();

    public string CurrentWord => _word.ToString();

    public event Action<CompletedWord>? WordCompleted;

    public void Reset() => _word.Clear();

    /// <summary>
    /// Feed a key-down event. Returns a completed word when a boundary is hit.
    /// </summary>
    public CompletedWord? Handle(TypedKey key)
    {
        if (key.IsInjected)
        {
            return null;
        }

        if (key.IsBackspace)
        {
            if (_word.Length > 0)
            {
                _word.Length--;
            }

            return null;
        }

        // Navigation/escape only apply when the key did not produce text.
        // Tests (and a few OEM keys) can reuse the same numeric vk codes as characters.
        if ((key.IsNavigation || key.IsEscape) && string.IsNullOrEmpty(key.Text))
        {
            Reset();
            return null;
        }

        // Shortcuts (copy/paste/select-all/etc.) mean we no longer know the buffer.
        if (key.HasModifier)
        {
            Reset();
            return null;
        }

        if (key.IsEnter || key.IsTab)
        {
            return Finish(key.IsEnter ? "\r" : "\t");
        }

        if (string.IsNullOrEmpty(key.Text))
        {
            return null;
        }

        var ch = key.Text[0];
        if (WordRules.IsWordCharacter(ch, _word.Length > 0))
        {
            _word.Append(key.Text);
            return null;
        }

        if (WordRules.IsDelimiter(ch))
        {
            return Finish(key.Text);
        }

        Reset();
        return null;
    }

    private CompletedWord? Finish(string trailing)
    {
        if (_word.Length == 0)
        {
            return null;
        }

        var completed = new CompletedWord(_word.ToString(), trailing);
        _word.Clear();
        WordCompleted?.Invoke(completed);
        return completed;
    }
}
