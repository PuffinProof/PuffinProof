using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Threading;
using PuffinProof.Core;

namespace PuffinProof.Services;

/// <summary>
/// Watches the focused text field via UI Automation (the same family of APIs
/// screen readers use). No keyboard or mouse hooks.
/// </summary>
public sealed class TextFieldWatcher : IDisposable
{
    private readonly DispatcherTimer _poll;
    private string _lastText = string.Empty;
    private int _lastCaret;
    private string? _lastRuntimeId;
    private FieldWord? _lastOffered;

    public event Action<FieldSnapshot>? WordCompleted;
    public Func<bool>? ShouldSkip { get; set; }

    public TextFieldWatcher()
    {
        _poll = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
        _poll.Tick += (_, _) => Poll();
    }

    public void Start()
    {
        Automation.AddAutomationFocusChangedEventHandler(OnFocusChanged);
        _poll.Start();
    }

    public void Stop()
    {
        _poll.Stop();
        try
        {
            Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChanged);
        }
        catch
        {
            // Ignore teardown races.
        }

        Reset();
    }

    public void Reset()
    {
        _lastText = string.Empty;
        _lastCaret = 0;
        _lastRuntimeId = null;
        _lastOffered = null;
    }

    public void Dispose() => Stop();

    private void OnFocusChanged(object? sender, AutomationFocusChangedEventArgs e)
    {
        var app = System.Windows.Application.Current;
        if (app is null)
        {
            return;
        }

        app.Dispatcher.BeginInvoke(Poll, DispatcherPriority.Background);
    }

    private void Poll()
    {
        try
        {
            if (ShouldSkip?.Invoke() == true)
            {
                Reset();
                return;
            }

            if (!TryReadFocusedField(out var snapshot))
            {
                Reset();
                return;
            }

            if (!string.Equals(snapshot.RuntimeId, _lastRuntimeId, StringComparison.Ordinal))
            {
                _lastRuntimeId = snapshot.RuntimeId;
                _lastText = snapshot.Text;
                _lastCaret = snapshot.Caret;
                _lastOffered = null;
                return;
            }

            if (snapshot.Text == _lastText && snapshot.Caret == _lastCaret)
            {
                return;
            }

            _lastText = snapshot.Text;
            _lastCaret = snapshot.Caret;

            var word = FieldText.CompletedWordBeforeCaret(snapshot.Text, snapshot.Caret);
            if (word is null)
            {
                return;
            }

            if (_lastOffered is not null &&
                _lastOffered.Start == word.Start &&
                _lastOffered.Word == word.Word &&
                _lastOffered.Trailing == word.Trailing)
            {
                return;
            }

            _lastOffered = word;
            WordCompleted?.Invoke(new FieldSnapshot(
                snapshot.Element,
                snapshot.Text,
                snapshot.Caret,
                word,
                snapshot.ScreenPoint));
        }
        catch
        {
            // Never let UIA failures take down the tray app.
        }
    }

    private static bool TryReadFocusedField(out LiveField field)
    {
        field = default!;
        AutomationElement? focused;
        try
        {
            focused = AutomationElement.FocusedElement;
        }
        catch
        {
            return false;
        }

        if (focused is null)
        {
            return false;
        }

        try
        {
            if (LooksLikeSecretField(focused))
            {
                return false;
            }

            if (!focused.Current.IsEnabled || focused.Current.IsOffscreen)
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        if (!TryGetTextAndCaret(focused, out var text, out var caret))
        {
            return false;
        }

        text = FieldText.Clamp(text);
        if (caret > text.Length)
        {
            caret = text.Length;
        }

        var id = string.Join('.', focused.GetRuntimeId());
        var rect = focused.Current.BoundingRectangle;
        var point = new Point(
            double.IsInfinity(rect.Left) ? 80 : rect.Left + 8,
            double.IsInfinity(rect.Bottom) ? 80 : rect.Bottom + 6);
        field = new LiveField(focused, text, caret, id, point);
        return true;
    }

    private static bool TryGetTextAndCaret(AutomationElement element, out string text, out int caret)
    {
        text = string.Empty;
        caret = 0;

        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var textObj) &&
            textObj is TextPattern textPattern)
        {
            text = textPattern.DocumentRange.GetText(FieldText.MaxReadLength) ?? string.Empty;
            caret = CaretFromTextPattern(textPattern, text.Length);
            return true;
        }

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valueObj) &&
            valueObj is ValuePattern valuePattern)
        {
            text = valuePattern.Current.Value ?? string.Empty;
            caret = text.Length;
            return !valuePattern.Current.IsReadOnly || text.Length > 0;
        }

        try
        {
            var name = element.Current.Name;
            if (!string.IsNullOrEmpty(name) &&
                element.Current.ControlType == ControlType.Edit)
            {
                text = name;
                caret = text.Length;
                return true;
            }
        }
        catch
        {
            // Fall through.
        }

        return false;
    }

    private static bool LooksLikeSecretField(AutomationElement element)
    {
        AutomationElement? current = element;
        for (var depth = 0; depth < 8 && current is not null; depth++)
        {
            AutomationElement.AutomationElementInformation info;
            try
            {
                info = current.Current;
            }
            catch
            {
                break;
            }

            if (info.IsPassword)
            {
                return true;
            }

            if (PasswordSignals.LooksSecret(
                    info.Name,
                    info.AutomationId,
                    info.ClassName,
                    info.LocalizedControlType,
                    info.HelpText))
            {
                return true;
            }

            try
            {
                current = TreeWalker.ControlViewWalker.GetParent(current);
            }
            catch
            {
                break;
            }
        }

        return false;
    }

    private static int CaretFromTextPattern(TextPattern pattern, int textLength)
    {
        try
        {
            var selection = pattern.GetSelection();
            if (selection.Length == 0)
            {
                return textLength;
            }

            var prefix = pattern.DocumentRange.Clone();
            prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End, selection[0], TextPatternRangeEndpoint.Start);
            return prefix.GetText(FieldText.MaxReadLength).Length;
        }
        catch
        {
            return textLength;
        }
    }

    private sealed record LiveField(
        AutomationElement Element,
        string Text,
        int Caret,
        string RuntimeId,
        Point ScreenPoint);
}

public sealed record FieldSnapshot(
    object Element,
    string Text,
    int Caret,
    FieldWord Word,
    Point ScreenPoint);
