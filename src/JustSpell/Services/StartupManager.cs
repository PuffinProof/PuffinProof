using System.IO;
using Microsoft.Win32;

namespace JustSpell.Services;

public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "JustSpell";

    public static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "JustSpell.exe");
            key.SetValue(ValueName, $"\"{exe}\"");
        }
        else if (key.GetValue(ValueName) is not null)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
