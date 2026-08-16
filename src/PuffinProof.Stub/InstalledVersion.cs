using Microsoft.Win32;

namespace PuffinProof.Stub;

public static class InstalledVersion
{
    public static string? Find()
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            foreach (var path in new[]
                     {
                         @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                         @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                     })
            {
                using var key = hive.OpenSubKey(path);
                if (key is null)
                {
                    continue;
                }

                foreach (var name in key.GetSubKeyNames())
                {
                    using var sub = key.OpenSubKey(name);
                    var display = sub?.GetValue("DisplayName") as string;
                    if (string.Equals(display, "PuffinProof", StringComparison.OrdinalIgnoreCase))
                    {
                        return sub?.GetValue("DisplayVersion") as string;
                    }
                }
            }
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"(Get-AppxPackage -Name PuffinProof).Version\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd()?.Trim();
            proc?.WaitForExit(4000);
            if (!string.IsNullOrWhiteSpace(output))
            {
                return output;
            }
        }
        catch
        {
            // Ignore.
        }

        return null;
    }
}
