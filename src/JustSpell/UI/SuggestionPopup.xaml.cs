using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using JustSpell.Core;
using JustSpell.Native;

namespace JustSpell.UI;

public partial class SuggestionPopup : Window
{
    private readonly DispatcherTimer _hideTimer = new();
    private CompletedWord? _current;

    public event Action<CompletedWord, string>? SuggestionChosen;
    public event Action<CompletedWord>? AddToDictionary;
    public event Action<CompletedWord>? Ignored;

    public bool IsShown => IsVisible;
    public CompletedWord? Current => _current;

    public SuggestionPopup()
    {
        InitializeComponent();
        _hideTimer.Tick += (_, _) => Dismiss();
        Loaded += OnLoaded;
        SourceInitialized += OnSourceInitialized;
    }

    public void ShowFor(CompletedWord word, IReadOnlyList<string> suggestions, Point screenPixels, TimeSpan lifetime)
    {
        _current = word;
        MisspelledText.Text = word.Word;

        var items = suggestions
            .Select((text, i) => new SuggestionRow(i + 1, text))
            .ToList();
        SuggestionList.ItemsSource = items;
        EmptyHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (!IsVisible)
        {
            Show();
        }

        PlaceAt(screenPixels);
        _hideTimer.Stop();
        _hideTimer.Interval = lifetime;
        _hideTimer.Start();
    }

    public bool TryChoose(int number)
    {
        if (!IsShown || SuggestionList.ItemsSource is not IEnumerable<SuggestionRow> rows)
        {
            return false;
        }

        var match = rows.FirstOrDefault(r => r.Number == number);
        if (match is null || _current is null)
        {
            return false;
        }

        Choose(match.Replacement);
        return true;
    }

    public void Dismiss()
    {
        _hideTimer.Stop();
        _current = null;
        Hide();
    }

    private void Choose(string replacement)
    {
        if (_current is null)
        {
            return;
        }

        var word = _current;
        Dismiss();
        SuggestionChosen?.Invoke(word, replacement);
    }

    private void OnSuggestionClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string replacement })
        {
            Choose(replacement);
        }
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (_current is null)
        {
            return;
        }

        var word = _current;
        Dismiss();
        AddToDictionary?.Invoke(word);
    }

    private void OnIgnoreClick(object sender, RoutedEventArgs e)
    {
        if (_current is null)
        {
            return;
        }

        var word = _current;
        Dismiss();
        Ignored?.Invoke(word);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var ex = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt32();
        ex |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, (IntPtr)ex);
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            handled = true;
            return (IntPtr)NativeMethods.MA_NOACTIVATE;
        }

        return IntPtr.Zero;
    }

    private void PlaceAt(Point screenPixels)
    {
        var source = PresentationSource.FromVisual(this);
        var fromDevice = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
        var dip = fromDevice.Transform(screenPixels);

        UpdateLayout();
        var width = ActualWidth > 0 ? ActualWidth : 280;
        var height = ActualHeight > 0 ? ActualHeight : 160;

        var work = SystemParameters.WorkArea;
        var left = dip.X;
        var top = dip.Y;
        if (left + width > work.Right)
        {
            left = work.Right - width - 8;
        }

        if (top + height > work.Bottom)
        {
            top = dip.Y - height - 24;
        }

        if (left < work.Left)
        {
            left = work.Left + 8;
        }

        if (top < work.Top)
        {
            top = work.Top + 8;
        }

        Left = left;
        Top = top;
    }

    public sealed record SuggestionRow(int Number, string Replacement);
}
