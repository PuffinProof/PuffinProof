namespace JustSpell.Core;

public readonly record struct TypedKey(
    int VirtualKey,
    string? Text,
    bool Ctrl,
    bool Alt,
    bool Shift,
    bool Win,
    bool IsInjected)
{
    public bool HasModifier => Ctrl || Alt || Win;

    public bool IsBackspace => VirtualKey == 8;

    public bool IsTab => VirtualKey == 9;

    public bool IsEnter => VirtualKey is 13;

    public bool IsEscape => VirtualKey == 27;

    public bool IsNavigation => VirtualKey is
        33 or 34 or 35 or 36 or // page up/down, end, home
        37 or 38 or 39 or 40 or // arrows
        46;                     // delete
}
