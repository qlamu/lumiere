namespace Lumiere.Models;

public class DisplayMonitor
{
    public required string DeviceName { get; init; }
    public required string DisplayName { get; init; }
    public required IntPtr PhysicalMonitorHandle { get; init; }
    public required IntPtr HMonitor { get; init; }

    public int MinBrightness { get; set; }
    public int MaxBrightness { get; set; } = 100;
    public int CurrentBrightness { get; set; }

    public bool SupportsDdcCi { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
