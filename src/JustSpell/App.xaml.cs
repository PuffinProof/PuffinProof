using System.Security.Principal;
using System.Windows;
using JustSpell.Core;
using JustSpell.Services;
using JustSpell.UI;

namespace JustSpell;

public partial class App : Application
{
    private Mutex? _mutex;
    private SettingsStore? _settings;
    private SpellEngine? _engine;
    private TextFieldWatcher? _watcher;
    private HotkeyService? _hotkeys;
    private SuggestionPopup? _popup;
    private TrayService? _tray;
    private SettingsWindow? _settingsWindow;
    private FieldSnapshot? _pending;
    private string? _lastTypedApp;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        if (IsElevated())
        {
            MessageBox.Show(
                "JustSpell does not use administrator rights. Close this window and start it normally — not “Run as administrator.”",
                "JustSpell",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
            return;
        }

        _mutex = new Mutex(true, @"Local\JustSpell.SingleInstance", out var created);
        if (!created)
        {
            MessageBox.Show("JustSpell is already running. Look for it in the system tray.", "JustSpell");
            Shutdown();
            return;
        }

        try
        {
            _settings = SettingsStore.Load();
            var user = new UserDictionary(AppPaths.UserWordsFile);
            _engine = SpellEngine.Load(AppPaths.BundledDictionariesDirectory, _settings.Current.Language, user);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "JustSpell could not load its dictionary.\n\n" + ex.Message,
                "JustSpell",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        _popup = new SuggestionPopup();
        _popup.SuggestionChosen += OnSuggestionChosen;
        _popup.AddToDictionary += OnAddToDictionary;
        _popup.Ignored += OnIgnored;

        _watcher = new TextFieldWatcher { ShouldSkip = ShouldSkipWatching };
        _watcher.WordCompleted += OnWordCompleted;
        _watcher.Start();

        _hotkeys = new HotkeyService();
        _hotkeys.TogglePressed += ToggleEnabled;
        _hotkeys.Install();

        _tray = new TrayService(_settings);
        _tray.ToggleRequested += ToggleEnabled;
        _tray.SettingsRequested += OpenSettings;
        _tray.DictionaryRequested += OpenDictionary;
        _tray.PauseInAppRequested += PauseInCurrentApp;
        _tray.ExitRequested += () => Shutdown();

        _settings.Changed += _ => _tray.Refresh();
        StartupManager.Apply(_settings.Current.StartWithWindows);

        if (_settings.Current.FirstRun)
        {
            _settings.Current.FirstRun = false;
            _settings.Save();
            _tray.ShowBalloon(
                "JustSpell is running",
                "Spellcheck only — watches the focused text field, no keyboard hook, no cloud.");
            OpenSettings();
        }
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _watcher?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _popup?.Close();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }

    private bool ShouldSkipWatching()
    {
        if (_settings is null || !_settings.Current.Enabled)
        {
            return true;
        }

        RememberForegroundApp();
        return ForegroundFilter.ShouldSkip(_settings.Current);
    }

    private void OnWordCompleted(FieldSnapshot snapshot)
    {
        if (_settings is null || _engine is null || _popup is null)
        {
            return;
        }

        if (_engine.Evaluate(snapshot.Word.Word, _settings.Current) != SpellDecision.Misspelled)
        {
            return;
        }

        _pending = snapshot;
        var suggestions = _engine.Suggest(snapshot.Word.Word);
        var lifetime = TimeSpan.FromSeconds(_settings.Current.PopupDurationSeconds);
        _popup.ShowFor(
            new CompletedWord(snapshot.Word.Word, snapshot.Word.Trailing),
            suggestions,
            snapshot.ScreenPoint,
            lifetime);
    }

    private void OnSuggestionChosen(CompletedWord word, string replacement)
    {
        if (_pending is null)
        {
            return;
        }

        var text = WordRules.ApplyCapitalization(word.Word, replacement);
        var ok = TextFieldEditor.TryReplace(_pending.Element, _pending.Word, text);
        _pending = null;
        if (!ok)
        {
            _tray?.ShowBalloon(
                "JustSpell",
                "This field didn't allow the correction. You can type “" + text + "” yourself.");
        }
    }

    private void OnAddToDictionary(CompletedWord word)
    {
        _pending = null;
        _engine?.AddToUserDictionary(word.Word);
    }

    private void OnIgnored(CompletedWord word)
    {
        _pending = null;
        _engine?.IgnoreForSession(word.Word);
    }

    private void ToggleEnabled()
    {
        if (_settings is null)
        {
            return;
        }

        _settings.Current.Enabled = !_settings.Current.Enabled;
        _settings.Save();
        _watcher?.Reset();
        _pending = null;
        _popup?.Dismiss();
        _tray?.Refresh();
    }

    private void OpenSettings()
    {
        if (_settings is null || _engine is null)
        {
            return;
        }

        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_settings, _engine);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
        _settingsWindow.FocusTryBox();
    }

    private void OpenDictionary()
    {
        if (_engine is null)
        {
            return;
        }

        var window = new DictionaryWindow(_engine.User);
        window.Show();
    }

    private void PauseInCurrentApp()
    {
        if (_settings is null)
        {
            return;
        }

        var name = _lastTypedApp ?? ForegroundFilter.CurrentProcessName();
        if (string.IsNullOrWhiteSpace(name) ||
            string.Equals(name, "JustSpell", StringComparison.OrdinalIgnoreCase))
        {
            _tray?.ShowBalloon("JustSpell", "Type in the app you want to skip, then try again.");
            return;
        }

        if (!_settings.Current.IsProcessExcluded(name))
        {
            _settings.Current.ExcludedProcesses.Add(name.ToLowerInvariant());
            _settings.Save();
        }

        _tray?.ShowBalloon("JustSpell", $"Paused in {name}. You can undo this in Settings.");
    }

    private void RememberForegroundApp()
    {
        var name = ForegroundFilter.CurrentProcessName();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (string.Equals(name, "JustSpell", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "explorer", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastTypedApp = name;
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
