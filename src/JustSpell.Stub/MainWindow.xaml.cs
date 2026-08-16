using System.Diagnostics;
using System.IO;
using System.Windows;
using JustSpell.Core;
using Microsoft.Win32;

namespace JustSpell.Stub;

public partial class MainWindow : Window
{
    private readonly FeedConfig _feed = FeedConfig.Load();
    private GitHubRelease? _latest;
    private string? _installed;
    private enum Mode { Check, Install }
    private Mode _mode = Mode.Check;

    public MainWindow()
    {
        InitializeComponent();
        _installed = InstalledVersion.Find();
        Status.Text = DescribeIdle();
    }

    private string DescribeIdle()
    {
        var installed = _installed is null ? "JustSpell is not installed." : "Installed version: " + _installed + ".";
        if (string.IsNullOrWhiteSpace(_feed.GithubRepo))
        {
            return installed +
                   " No GitHub repo in feed.json (or JUSTSPELL_GITHUB_REPO). You can still install a local MSIX.";
        }

        return installed + " Feed: " + _feed.GithubRepo + ".";
    }

    private async void OnPrimary(object sender, RoutedEventArgs e)
    {
        PrimaryButton.IsEnabled = false;
        try
        {
            if (_mode == Mode.Check)
            {
                await CheckAsync().ConfigureAwait(true);
            }
            else
            {
                await InstallLatestAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            Status.Text = "Could not finish: " + ex.Message;
        }
        finally
        {
            PrimaryButton.IsEnabled = true;
        }
    }

    private async Task CheckAsync()
    {
        Status.Text = "Asking GitHub for the latest release…";
        Progress.Value = 0;
        _latest = await GitHubRelease.FetchLatestAsync(_feed, CancellationToken.None).ConfigureAwait(true);
        if (_latest is null)
        {
            Status.Text = DescribeIdle() +
                          " No release asset named " + _feed.AssetName +
                          " was found. Use a local MSIX or publish a GitHub Release.";
            return;
        }

        if (!VersionCompare.IsNewer(_latest.Version, _installed))
        {
            Status.Text = "You already have " + (_installed ?? _latest.Version) +
                          ". Latest on GitHub is " + _latest.Tag + ". Nothing to download.";
            _mode = Mode.Check;
            PrimaryButton.Content = "Check for latest";
            return;
        }

        Status.Text = "Latest on GitHub is " + _latest.Tag +
                      (_installed is null ? "." : ". You have " + _installed + ".") +
                      " Click Install to download the MSIX and register it with Windows.";
        _mode = Mode.Install;
        PrimaryButton.Content = "Install " + _latest.Tag;
    }

    private async Task InstallLatestAsync()
    {
        if (_latest is null)
        {
            await CheckAsync().ConfigureAwait(true);
            return;
        }

        Status.Text = "Downloading " + _latest.Tag + "…";
        var progress = new Progress<double>(v => Progress.Value = v);
        var msix = await GitHubRelease.DownloadAsync(_latest.DownloadUrl, progress, CancellationToken.None)
            .ConfigureAwait(true);
        LaunchMsix(msix);
        Status.Text = "Windows is installing the MSIX. JustSpell does not need administrator rights.";
    }

    private void OnLocalMsi(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "MSIX package (*.msix)|*.msix|App Installer (*.appinstaller)|*.appinstaller",
            Title = "Choose a JustSpell package"
        };
        var local = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "dist", "JustSpell.msix"));
        if (File.Exists(local))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(local);
            dialog.FileName = local;
        }

        if (dialog.ShowDialog(this) == true)
        {
            LaunchMsix(dialog.FileName);
        }
    }

    private static void LaunchMsix(string path)
    {
        var args = path.EndsWith(".appinstaller", StringComparison.OrdinalIgnoreCase)
            ? "-NoProfile -Command \"Add-AppxPackage -AppInstallerFile '" + path.Replace("'", "''") + "'\""
            : "-NoProfile -Command \"Add-AppxPackage -Path '" + path.Replace("'", "''") + "' -AllowUnsigned\"";

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = args,
            UseShellExecute = true
        });
    }
}
