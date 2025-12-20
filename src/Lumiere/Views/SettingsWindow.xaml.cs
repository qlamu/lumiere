using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Lumiere.Models;
using Lumiere.Services;

namespace Lumiere.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private bool _isLoading = true;

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

    public SettingsWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
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

        int dayValue = dayPreset?.MonitorBrightnessLevels.GetValueOrDefault("*", 100) ?? 100;
        int nightValue = nightPreset?.MonitorBrightnessLevels.GetValueOrDefault("*", 30) ?? 30;

        DaySlider.Value = dayValue;
        NightSlider.Value = nightValue;

        DayValueLabel.Text = $"{dayValue}%";
        NightValueLabel.Text = $"{nightValue}%";
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
}
