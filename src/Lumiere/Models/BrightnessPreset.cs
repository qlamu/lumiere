namespace Lumiere.Models;

public class BrightnessPreset
{
    public required string Name { get; set; }
    public Dictionary<string, int> MonitorBrightnessLevels { get; set; } = new();

    public static BrightnessPreset Day => new()
    {
        Name = "Day",
        MonitorBrightnessLevels = new() { { "*", 100 } }
    };

    public static BrightnessPreset Night => new()
    {
        Name = "Night",
        MonitorBrightnessLevels = new() { { "*", 30 } }
    };
}
