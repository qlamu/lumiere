using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace Lumiere.Native;

public static class TrayIconHelper
{
    private const string ThemeRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool IsLightTaskbar()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ThemeRegistryKey, false);
            if (key?.GetValue("SystemUsesLightTheme") is int value)
            {
                return value == 1;
            }
        }
        catch
        {
            // Default to dark taskbar
        }
        return false;
    }

    public static Icon CreateSunIcon()
    {
        bool lightTaskbar = IsLightTaskbar();
        // Light taskbar: softer gray, thinner strokes
        // Dark taskbar: white, bolder strokes
        var color = lightTaskbar ? Color.FromArgb(90, 90, 90) : Color.White;
        float rayWidth = lightTaskbar ? 1.8f : 2.5f;
        return CreateSunIcon(color, rayWidth);
    }

    public static Icon CreateSunIcon(Color color, float rayWidth = 2.5f)
    {
        const int size = 32;
        const int center = size / 2;
        const int sunRadius = 7;
        const int rayInner = 9;
        const int rayOuter = 14;

        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(color);
        using var pen = new Pen(color, rayWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        // Draw sun circle
        g.FillEllipse(brush, center - sunRadius, center - sunRadius, sunRadius * 2, sunRadius * 2);

        // Draw 8 rays
        for (int i = 0; i < 8; i++)
        {
            double angle = i * Math.PI / 4;
            float x1 = center + (float)(rayInner * Math.Cos(angle));
            float y1 = center + (float)(rayInner * Math.Sin(angle));
            float x2 = center + (float)(rayOuter * Math.Cos(angle));
            float y2 = center + (float)(rayOuter * Math.Sin(angle));
            g.DrawLine(pen, x1, y1, x2, y2);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }
}
