using System.Diagnostics;
using System.Text;
using JustSpell.Core;
using JustSpell.Native;

namespace JustSpell.Services;

public static class ForegroundFilter
{
    public static bool ShouldSkip(AppSettings settings)
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return true;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
        {
            return true;
        }

        try
        {
            using var process = Process.GetProcessById((int)pid);
            var name = process.ProcessName;
            if (string.Equals(name, "JustSpell", StringComparison.OrdinalIgnoreCase))
            {
                // Still allow the in-app try-it box.
                return false;
            }

            if (settings.IsProcessExcluded(name))
            {
                return true;
            }
        }
        catch
        {
            return true;
        }

        if (LooksLikePasswordField(hwnd))
        {
            return true;
        }

        var title = new StringBuilder(512);
        NativeMethods.GetWindowText(hwnd, title, title.Capacity);
        return PasswordSignals.LooksSecret(title.ToString());
    }

    public static string? CurrentProcessName()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksLikePasswordField(IntPtr hwnd)
    {
        var threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        var info = new NativeMethods.GUITHREADINFO
        {
            cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.GUITHREADINFO>()
        };
        if (!NativeMethods.GetGUIThreadInfo(threadId, ref info))
        {
            return false;
        }

        var focus = info.hwndFocus != IntPtr.Zero ? info.hwndFocus : hwnd;
        var className = new StringBuilder(256);
        NativeMethods.GetClassName(focus, className, className.Capacity);
        var name = className.ToString();
        if (name.Contains("Password", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var style = NativeMethods.GetWindowLongPtr(focus, NativeMethods.GWL_STYLE).ToInt64();
        return (style & NativeMethods.ES_PASSWORD) != 0;
    }
}
