using System.Windows.Interop;
using Lumiere.Models;
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
    private SettingsService? _settingsService;
    private bool _disposed;

    public void Initialize(MainViewModel viewModel, SettingsService settingsService)
    {
        _viewModel = viewModel;
        _settingsService = settingsService;

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

        RegisterHotkeys();
    }

    public void ReregisterHotkeys()
    {
        UnregisterHotkeys();
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        if (_hwndSource == null || _settingsService == null) return;

        var handle = _hwndSource.Handle;
        var hotkeys = _settingsService.Settings.Hotkeys;

        RegisterHotkey(handle, HOTKEY_BRIGHTNESS_UP, hotkeys.BrightnessUp);
        RegisterHotkey(handle, HOTKEY_BRIGHTNESS_DOWN, hotkeys.BrightnessDown);
        RegisterHotkey(handle, HOTKEY_DAY_PRESET, hotkeys.DayPreset);
        RegisterHotkey(handle, HOTKEY_NIGHT_PRESET, hotkeys.NightPreset);
    }

    private void RegisterHotkey(IntPtr handle, int id, HotkeyBinding binding)
    {
        if (!binding.IsEnabled || string.IsNullOrEmpty(binding.Key)) return;

        var modifiers = ParseModifiers(binding.Modifiers);
        var key = ParseKey(binding.Key);

        if (key == 0) return;

        User32Interop.RegisterHotKey(handle, id, modifiers, (uint)key);
    }

    private static uint ParseModifiers(string modifierString)
    {
        if (string.IsNullOrEmpty(modifierString)) return 0;

        uint result = 0;
        var parts = modifierString.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            result |= part.ToUpperInvariant() switch
            {
                "CTRL" => HotkeyModifiers.Control,
                "ALT" => HotkeyModifiers.Alt,
                "SHIFT" => HotkeyModifiers.Shift,
                "WIN" => HotkeyModifiers.Win,
                _ => 0
            };
        }

        return result;
    }

    private static int ParseKey(string keyString)
    {
        if (string.IsNullOrEmpty(keyString)) return 0;

        return keyString.Trim().ToUpperInvariant() switch
        {
            // Arrow keys
            "UP" => VirtualKeys.Up,
            "DOWN" => VirtualKeys.Down,
            "LEFT" => VirtualKeys.Left,
            "RIGHT" => VirtualKeys.Right,

            // Letters
            "A" => VirtualKeys.A, "B" => VirtualKeys.B, "C" => VirtualKeys.C, "D" => VirtualKeys.D,
            "E" => VirtualKeys.E, "F" => VirtualKeys.F, "G" => VirtualKeys.G, "H" => VirtualKeys.H,
            "I" => VirtualKeys.I, "J" => VirtualKeys.J, "K" => VirtualKeys.K, "L" => VirtualKeys.L,
            "M" => VirtualKeys.M, "N" => VirtualKeys.N, "O" => VirtualKeys.O, "P" => VirtualKeys.P,
            "Q" => VirtualKeys.Q, "R" => VirtualKeys.R, "S" => VirtualKeys.S, "T" => VirtualKeys.T,
            "U" => VirtualKeys.U, "V" => VirtualKeys.V, "W" => VirtualKeys.W, "X" => VirtualKeys.X,
            "Y" => VirtualKeys.Y, "Z" => VirtualKeys.Z,

            // Numbers
            "0" => VirtualKeys.D0, "1" => VirtualKeys.D1, "2" => VirtualKeys.D2, "3" => VirtualKeys.D3,
            "4" => VirtualKeys.D4, "5" => VirtualKeys.D5, "6" => VirtualKeys.D6, "7" => VirtualKeys.D7,
            "8" => VirtualKeys.D8, "9" => VirtualKeys.D9,

            // Function keys
            "F1" => VirtualKeys.F1, "F2" => VirtualKeys.F2, "F3" => VirtualKeys.F3, "F4" => VirtualKeys.F4,
            "F5" => VirtualKeys.F5, "F6" => VirtualKeys.F6, "F7" => VirtualKeys.F7, "F8" => VirtualKeys.F8,
            "F9" => VirtualKeys.F9, "F10" => VirtualKeys.F10, "F11" => VirtualKeys.F11, "F12" => VirtualKeys.F12,

            // Special keys
            "SPACE" => VirtualKeys.Space,
            "TAB" => VirtualKeys.Tab,
            "ENTER" => VirtualKeys.Enter,
            "BACKSPACE" => VirtualKeys.Back,
            "DELETE" => VirtualKeys.Delete,
            "INSERT" => VirtualKeys.Insert,
            "HOME" => VirtualKeys.Home,
            "END" => VirtualKeys.End,
            "PAGEUP" => VirtualKeys.PageUp,
            "PAGEDOWN" => VirtualKeys.PageDown,

            // Numpad
            "NUMPAD0" => VirtualKeys.NumPad0, "NUMPAD1" => VirtualKeys.NumPad1, "NUMPAD2" => VirtualKeys.NumPad2,
            "NUMPAD3" => VirtualKeys.NumPad3, "NUMPAD4" => VirtualKeys.NumPad4, "NUMPAD5" => VirtualKeys.NumPad5,
            "NUMPAD6" => VirtualKeys.NumPad6, "NUMPAD7" => VirtualKeys.NumPad7, "NUMPAD8" => VirtualKeys.NumPad8,
            "NUMPAD9" => VirtualKeys.NumPad9,
            "NUMPAD+" => VirtualKeys.NumPadAdd,
            "NUMPAD-" => VirtualKeys.NumPadSubtract,
            "NUMPAD*" => VirtualKeys.NumPadMultiply,
            "NUMPAD/" => VirtualKeys.NumPadDivide,

            // OEM keys
            "+" => VirtualKeys.OemPlus,
            "-" => VirtualKeys.OemMinus,
            "," => VirtualKeys.OemComma,
            "." => VirtualKeys.OemPeriod,

            _ => 0
        };
    }

    public static string FormatBinding(HotkeyBinding binding)
    {
        if (!binding.IsEnabled || string.IsNullOrEmpty(binding.Key))
            return "None";

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(binding.Modifiers))
        {
            parts.AddRange(binding.Modifiers.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        parts.Add(binding.Key);

        return string.Join(" + ", parts);
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
