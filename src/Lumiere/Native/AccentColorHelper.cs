using System.Windows.Media;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;

namespace Lumiere.Native;

public static class AccentColorHelper
{
    public static SolidColorBrush AccentBrush
    {
        get
        {
            // Don't cache - accent varies by theme
            var brush = new SolidColorBrush(GetAccentColor());
            brush.Freeze();
            return brush;
        }
    }

    public static Color GetAccentColor()
    {
        bool isLightTheme = TrayIconHelper.IsLightTaskbar();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            if (key?.GetValue("AccentPalette") is byte[] palette && palette.Length >= 16)
            {
                // AccentPalette contains 8 RGBA colors (4 bytes each), lightest to darkest
                // Each color is stored as R, G, B, A
                // Dark mode: index 1 (bytes 4-7) - lighter shade
                // Light mode: index 3 (bytes 12-15) - darker shade for contrast
                int offset = isLightTheme ? 12 : 4;
                return Color.FromRgb(palette[offset], palette[offset + 1], palette[offset + 2]);
            }

            // Fallback to DWM AccentColor
            using var dwmKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\DWM");
            if (dwmKey?.GetValue("AccentColor") is int accentColor)
            {
                byte b = (byte)((accentColor >> 16) & 0xFF);
                byte g = (byte)((accentColor >> 8) & 0xFF);
                byte r = (byte)(accentColor & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch
        {
            // Fallback silently
        }

        // Default Windows blue accent
        return Color.FromRgb(0, 120, 212);
    }
}
