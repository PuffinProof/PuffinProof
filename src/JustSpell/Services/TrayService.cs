using System.IO;
using JustSpell.Core;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace JustSpell.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _enabledItem;
    private readonly SettingsStore _settings;

    public event Action? ToggleRequested;
    public event Action? SettingsRequested;
    public event Action? DictionaryRequested;
    public event Action? PauseInAppRequested;
    public event Action? ExitRequested;

    public TrayService(SettingsStore settings)
    {
        _settings = settings;
        _enabledItem = new Forms.ToolStripMenuItem("Spellcheck on")
        {
            CheckOnClick = false
        };
        _enabledItem.Click += (_, _) => ToggleRequested?.Invoke();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_enabledItem);
        menu.Items.Add("Pause in this app", null, (_, _) => PauseInAppRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Settings…", null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add("Your words…", null, (_, _) => DictionaryRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit JustSpell", null, (_, _) => ExitRequested?.Invoke());

        _icon = new Forms.NotifyIcon
        {
            Text = "JustSpell",
            Visible = true,
            ContextMenuStrip = menu,
            Icon = LoadIcon()
        };
        _icon.DoubleClick += (_, _) => SettingsRequested?.Invoke();
        Refresh();
    }

    public void Refresh()
    {
        var on = _settings.Current.Enabled;
        _enabledItem.Checked = on;
        _enabledItem.Text = on ? "Spellcheck on" : "Spellcheck off";
        _icon.Text = on ? "JustSpell — on" : "JustSpell — paused";
    }

    public void ShowBalloon(string title, string text)
    {
        _icon.BalloonTipTitle = title;
        _icon.BalloonTipText = text;
        _icon.ShowBalloonTip(4000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    private static Drawing.Icon LoadIcon()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe))
        {
            var extracted = Drawing.Icon.ExtractAssociatedIcon(exe);
            if (extracted is not null)
            {
                return extracted;
            }
        }

        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "justspell.ico");
        if (File.Exists(path))
        {
            return new Drawing.Icon(path);
        }

        return Drawing.SystemIcons.Information;
    }
}
