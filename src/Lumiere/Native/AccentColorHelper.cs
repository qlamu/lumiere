using System.Windows.Media;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;

namespace Lumiere.Native;

public static class AccentColorHelper
{
    private static SolidColorBrush? _accentBrush;

    public static SolidColorBrush AccentBrush
    {
        get
        {
            if (_accentBrush == null)
            {
                _accentBrush = new SolidColorBrush(GetAccentColor());
                _accentBrush.Freeze();
            }
            return _accentBrush;
        }
    }

    public static Color GetAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int accentColor)
            {
                // AccentColor is stored as ABGR (alpha, blue, green, red)
                byte a = (byte)((accentColor >> 24) & 0xFF);
                byte b = (byte)((accentColor >> 16) & 0xFF);
                byte g = (byte)((accentColor >> 8) & 0xFF);
                byte r = (byte)(accentColor & 0xFF);

                // Use full opacity for UI elements
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
