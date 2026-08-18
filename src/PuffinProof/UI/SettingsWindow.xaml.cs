using System.Windows;
using PuffinProof.Core;
using PuffinProof.Services;

namespace PuffinProof.UI;

public partial class SettingsWindow : Window
{
    private readonly SettingsStore _settings;
    private readonly SpellEngine _engine;

    public SettingsWindow(SettingsStore settings, SpellEngine engine)
    {
        InitializeComponent();
        _settings = settings;
        _engine = engine;
        LoadFromSettings();
        VersionText.Text = "Version " + AppVersion.Display;
        Closing += (_, _) => Save();
    }

    public void FocusTryBox()
    {
        Show();
        Activate();
        TryBox.Focus();
    }

    private void LoadFromSettings()
    {
        var s = _settings.Current;
        EnabledBox.IsChecked = s.Enabled;
        StartupBox.IsChecked = s.StartWithWindows;
        IgnoreDigitsBox.IsChecked = s.IgnoreWordsWithDigits;
        MinLengthBox.Text = s.MinWordLength.ToString();
        DurationBox.Text = s.PopupDurationSeconds.ToString();
        ExcludeBox.Text = string.Join(Environment.NewLine, s.ExcludedProcesses);

        var languages = SpellEngine.AvailableLanguages(AppPaths.BundledDictionariesDirectory);
        LanguageBox.ItemsSource = languages.Count > 0 ? languages : new[] { s.Language };
        LanguageBox.SelectedItem = languages.Contains(s.Language) ? s.Language : LanguageBox.Items[0];
    }

    private void Save()
    {
        var s = _settings.Current;
        s.Enabled = EnabledBox.IsChecked == true;
        s.StartWithWindows = StartupBox.IsChecked == true;
        s.IgnoreWordsWithDigits = IgnoreDigitsBox.IsChecked == true;
        s.Language = LanguageBox.SelectedItem as string ?? s.Language;
        if (int.TryParse(MinLengthBox.Text, out var min))
        {
            s.MinWordLength = min;
        }

        if (int.TryParse(DurationBox.Text, out var seconds))
        {
            s.PopupDurationSeconds = seconds;
        }

        s.ExcludedProcesses = ExcludeBox.Text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        s.Normalize();
        _settings.Save();
        StartupManager.Apply(s.StartWithWindows);
    }

    private void OnEditWords(object sender, RoutedEventArgs e)
    {
        var window = new DictionaryWindow(_engine.User) { Owner = this };
        window.ShowDialog();
    }
}
