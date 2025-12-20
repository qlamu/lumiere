using System.Windows;
using System.Windows.Interop;
using Lumiere.Native;
using Lumiere.ViewModels;

namespace Lumiere.Services;

public class HotkeyService : IDisposable
{
    private const int HOTKEY_BRIGHTNESS_UP = 1;
    private const int HOTKEY_BRIGHTNESS_DOWN = 2;
    private const int HOTKEY_DAY_PRESET = 3;
    private const int HOTKEY_NIGHT_PRESET = 4;

    private HwndSource? _hwndSource;
    private MainViewModel? _viewModel;
    private bool _disposed;

    public void Initialize(MainViewModel viewModel)
    {
        _viewModel = viewModel;

        // Create a hidden window for receiving hotkey messages
        var parameters = new HwndSourceParameters("LumiereHotkeyWindow")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        RegisterDefaultHotkeys();
    }

    private void RegisterDefaultHotkeys()
    {
        if (_hwndSource == null) return;

        var handle = _hwndSource.Handle;

        // Ctrl+Alt+Up - Brightness up
        User32Interop.RegisterHotKey(handle, HOTKEY_BRIGHTNESS_UP,
            HotkeyModifiers.Control | HotkeyModifiers.Alt, (uint)VirtualKeys.Up);

        // Ctrl+Alt+Down - Brightness down
        User32Interop.RegisterHotKey(handle, HOTKEY_BRIGHTNESS_DOWN,
            HotkeyModifiers.Control | HotkeyModifiers.Alt, (uint)VirtualKeys.Down);

        // Ctrl+Alt+D - Day preset
        User32Interop.RegisterHotKey(handle, HOTKEY_DAY_PRESET,
            HotkeyModifiers.Control | HotkeyModifiers.Alt, (uint)VirtualKeys.D);

        // Ctrl+Alt+N - Night preset
        User32Interop.RegisterHotKey(handle, HOTKEY_NIGHT_PRESET,
            HotkeyModifiers.Control | HotkeyModifiers.Alt, (uint)VirtualKeys.N);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WindowMessages.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();

            switch (hotkeyId)
            {
                case HOTKEY_BRIGHTNESS_UP:
                    _viewModel?.AdjustBrightness(5);
                    handled = true;
                    break;
                case HOTKEY_BRIGHTNESS_DOWN:
                    _viewModel?.AdjustBrightness(-5);
                    handled = true;
                    break;
                case HOTKEY_DAY_PRESET:
                    _viewModel?.ApplyPresetByName("Day");
                    handled = true;
                    break;
                case HOTKEY_NIGHT_PRESET:
                    _viewModel?.ApplyPresetByName("Night");
                    handled = true;
                    break;
            }
        }

        return IntPtr.Zero;
    }

    private void UnregisterHotkeys()
    {
        if (_hwndSource == null) return;

        var handle = _hwndSource.Handle;
        User32Interop.UnregisterHotKey(handle, HOTKEY_BRIGHTNESS_UP);
        User32Interop.UnregisterHotKey(handle, HOTKEY_BRIGHTNESS_DOWN);
        User32Interop.UnregisterHotKey(handle, HOTKEY_DAY_PRESET);
        User32Interop.UnregisterHotKey(handle, HOTKEY_NIGHT_PRESET);
    }

    public void Dispose()
    {
        if (_disposed) return;

        UnregisterHotkeys();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~HotkeyService()
    {
        Dispose();
    }
}
