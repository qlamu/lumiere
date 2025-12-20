namespace Lumiere.Models;

public class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; } = true;

    public List<BrightnessPreset> Presets { get; set; } = new()
    {
        BrightnessPreset.Day,
        BrightnessPreset.Night,
        new BrightnessPreset { Name = "Custom", MonitorBrightnessLevels = new() { { "*", 50 } } }
    };

    public Dictionary<string, int> LastBrightnessValues { get; set; } = new();

    public HotkeySettings Hotkeys { get; set; } = new();
}

public class HotkeySettings
{
    public HotkeyBinding BrightnessUp { get; set; } = new() { Modifiers = "Ctrl+Alt", Key = "Up" };
    public HotkeyBinding BrightnessDown { get; set; } = new() { Modifiers = "Ctrl+Alt", Key = "Down" };
    public HotkeyBinding DayPreset { get; set; } = new() { Modifiers = "Ctrl+Alt", Key = "D" };
    public HotkeyBinding NightPreset { get; set; } = new() { Modifiers = "Ctrl+Alt", Key = "N" };
}

public class HotkeyBinding
{
    public string Modifiers { get; set; } = "";
    public string Key { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}
