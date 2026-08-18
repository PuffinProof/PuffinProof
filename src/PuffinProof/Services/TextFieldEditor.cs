using System.Windows.Automation;
using PuffinProof.Core;

namespace PuffinProof.Services;

public static class TextFieldEditor
{
    public static bool TryReplace(object elementObj, FieldWord word, string replacement)
    {
        if (elementObj is not AutomationElement element ||
            string.IsNullOrEmpty(word.Word) ||
            !WordRules.IsSafeReplacement(replacement))
        {
            return false;
        }

        if (!TryReadLiveText(element, out var live))
        {
            return false;
        }

        var span = FieldText.LocateForReplace(live, word);
        if (span is null)
        {
            return false;
        }

        var next = live[..span.Start] + replacement + span.Trailing + live[span.End..];
        if (next == live)
        {
            return false;
        }

        try
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valueObj) &&
                valueObj is ValuePattern { Current.IsReadOnly: false } value)
            {
                value.SetValue(next);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryReadLiveText(AutomationElement element, out string text)
    {
        text = string.Empty;
        try
        {
            if (element.Current.IsPassword)
            {
                return false;
            }

            if (element.TryGetCurrentPattern(TextPattern.Pattern, out var textObj) &&
                textObj is TextPattern textPattern)
            {
                text = FieldText.Clamp(textPattern.DocumentRange.GetText(FieldText.MaxReadLength) ?? string.Empty);
                return text.Length > 0;
            }

            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valueObj) &&
                valueObj is ValuePattern valuePattern &&
                !valuePattern.Current.IsReadOnly)
            {
                text = FieldText.Clamp(valuePattern.Current.Value ?? string.Empty);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
