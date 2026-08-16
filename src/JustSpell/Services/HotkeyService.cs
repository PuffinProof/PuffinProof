using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace JustSpell.Services;

/// <summary>
/// One official Windows hotkey (RegisterHotKey). Not a keyboard hook.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    public const int ToggleId = 1;
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VkS = 0x53;

    private HwndSource? _source;

    public event Action? TogglePressed;

    public void Install()
    {
        if (_source is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("JustSpellHotkeys")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = unchecked((int)0x80000000) // WS_POPUP
        };
        _source = new HwndSource(parameters);
        _source.AddHook(Hook);
        if (!RegisterHotKey(_source.Handle, ToggleId, ModControl | ModAlt, VkS))
        {
            // Another app owns Ctrl+Alt+S. Toggle still works from the tray.
        }
    }

    public void Dispose()
    {
        if (_source is not null)
        {
            UnregisterHotKey(_source.Handle, ToggleId);
            _source.RemoveHook(Hook);
            _source.Dispose();
            _source = null;
        }
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == ToggleId)
        {
            TogglePressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
