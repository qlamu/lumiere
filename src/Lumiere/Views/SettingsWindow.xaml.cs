using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Lumiere.Models;
using Lumiere.Services;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Lumiere.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly HotkeyService? _hotkeyService;
    private bool _isLoading = true;
    private Button? _capturingButton;
    private string? _originalButtonContent;

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMSBT_TABBEDWINDOW = 4; // Mica Alt
    private const int DWMWCP_ROUND = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int Left, Right, Top, Bottom;
        public MARGINS(int value) => Left = Right = Top = Bottom = value;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    public SettingsWindow(SettingsService settingsService, HotkeyService? hotkeyService = null)
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        PreviewKeyDown += OnPreviewKeyDown;
        LoadSettings();
        _isLoading = false;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        EnableMica();
    }

    private void EnableMica()
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        // Enable dark mode
        int darkMode = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        // Enable rounded corners
        int cornerPreference = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

        // Extend frame into client area (required for Mica)
        var margins = new MARGINS(-1);
        DwmExtendFrameIntoClientArea(hwnd, ref margins);

        // Enable Mica backdrop
        int micaValue = DWMSBT_TABBEDWINDOW;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref micaValue, sizeof(int));
    }

    private void LoadSettings()
    {
        var dayPreset = _settingsService.GetPreset("Day");
        var nightPreset = _settingsService.GetPreset("Night");

        int dayValue = dayPreset?.MonitorBrightnessLevels.GetValueOrDefault("*", 75) ?? 75;
        int nightValue = nightPreset?.MonitorBrightnessLevels.GetValueOrDefault("*", 30) ?? 30;

        DaySlider.Value = dayValue;
        NightSlider.Value = nightValue;

        DayValueLabel.Text = $"{dayValue}%";
        NightValueLabel.Text = $"{nightValue}%";

        // Load hotkey bindings
        var hotkeys = _settingsService.Settings.Hotkeys;
        BrightnessUpButton.Content = HotkeyService.FormatBinding(hotkeys.BrightnessUp);
        BrightnessDownButton.Content = HotkeyService.FormatBinding(hotkeys.BrightnessDown);
        DayPresetButton.Content = HotkeyService.FormatBinding(hotkeys.DayPreset);
        NightPresetButton.Content = HotkeyService.FormatBinding(hotkeys.NightPreset);
    }

    private void DaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading || DayValueLabel == null) return;

        int value = (int)e.NewValue;
        DayValueLabel.Text = $"{value}%";

        var preset = new BrightnessPreset
        {
            Name = "Day",
            MonitorBrightnessLevels = new() { { "*", value } }
        };
        _settingsService.SavePreset(preset);
    }

    private void NightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading || NightValueLabel == null) return;

        int value = (int)e.NewValue;
        NightValueLabel.Text = $"{value}%";

        var preset = new BrightnessPreset
        {
            Name = "Night",
            MonitorBrightnessLevels = new() { { "*", value } }
        };
        _settingsService.SavePreset(preset);
    }

    private void HotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        // If already capturing, cancel
        if (_capturingButton == button)
        {
            CancelCapture();
            return;
        }

        // Cancel any previous capture
        if (_capturingButton != null)
        {
            CancelCapture();
        }

        // Start capture mode
        _capturingButton = button;
        _originalButtonContent = button.Content?.ToString();
        button.Content = "Press a key...";
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_capturingButton == null) return;

        e.Handled = true;

        // Escape cancels
        if (e.Key == Key.Escape)
        {
            CancelCapture();
            return;
        }

        // Get the actual key (not modifier)
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore if only modifier keys pressed
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // Build modifier string
        var modifiers = new List<string>();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers.Add("Ctrl");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers.Add("Alt");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers.Add("Shift");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers.Add("Win");

        // Convert key to string
        var keyString = ConvertKeyToString(key);
        if (keyString == null)
        {
            CancelCapture();
            return;
        }

        // Create binding
        var binding = new HotkeyBinding
        {
            Modifiers = string.Join("+", modifiers),
            Key = keyString,
            IsEnabled = true
        };

        // Save to settings
        SaveHotkeyBinding(_capturingButton, binding);

        // Update button display
        _capturingButton.Content = HotkeyService.FormatBinding(binding);
        _capturingButton = null;
        _originalButtonContent = null;

        // Reregister hotkeys
        _hotkeyService?.ReregisterHotkeys();
    }

    private void CancelCapture()
    {
        if (_capturingButton != null && _originalButtonContent != null)
        {
            _capturingButton.Content = _originalButtonContent;
        }
        _capturingButton = null;
        _originalButtonContent = null;
    }

    private void SaveHotkeyBinding(Button button, HotkeyBinding binding)
    {
        var hotkeys = _settingsService.Settings.Hotkeys;

        if (button == BrightnessUpButton)
            hotkeys.BrightnessUp = binding;
        else if (button == BrightnessDownButton)
            hotkeys.BrightnessDown = binding;
        else if (button == DayPresetButton)
            hotkeys.DayPreset = binding;
        else if (button == NightPresetButton)
            hotkeys.NightPreset = binding;

        _settingsService.Save();
    }

    private static string? ConvertKeyToString(Key key)
    {
        return key switch
        {
            // Arrow keys
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",

            // Letters
            Key.A => "A", Key.B => "B", Key.C => "C", Key.D => "D",
            Key.E => "E", Key.F => "F", Key.G => "G", Key.H => "H",
            Key.I => "I", Key.J => "J", Key.K => "K", Key.L => "L",
            Key.M => "M", Key.N => "N", Key.O => "O", Key.P => "P",
            Key.Q => "Q", Key.R => "R", Key.S => "S", Key.T => "T",
            Key.U => "U", Key.V => "V", Key.W => "W", Key.X => "X",
            Key.Y => "Y", Key.Z => "Z",

            // Numbers
            Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3",
            Key.D4 => "4", Key.D5 => "5", Key.D6 => "6", Key.D7 => "7",
            Key.D8 => "8", Key.D9 => "9",

            // Function keys
            Key.F1 => "F1", Key.F2 => "F2", Key.F3 => "F3", Key.F4 => "F4",
            Key.F5 => "F5", Key.F6 => "F6", Key.F7 => "F7", Key.F8 => "F8",
            Key.F9 => "F9", Key.F10 => "F10", Key.F11 => "F11", Key.F12 => "F12",

            // Special keys
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Enter => "Enter",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",

            // Numpad
            Key.NumPad0 => "NumPad0", Key.NumPad1 => "NumPad1", Key.NumPad2 => "NumPad2",
            Key.NumPad3 => "NumPad3", Key.NumPad4 => "NumPad4", Key.NumPad5 => "NumPad5",
            Key.NumPad6 => "NumPad6", Key.NumPad7 => "NumPad7", Key.NumPad8 => "NumPad8",
            Key.NumPad9 => "NumPad9",
            Key.Add => "NumPad+",
            Key.Subtract => "NumPad-",
            Key.Multiply => "NumPad*",
            Key.Divide => "NumPad/",

            // OEM keys
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.OemComma => ",",
            Key.OemPeriod => ".",

            _ => null
        };
    }
}
