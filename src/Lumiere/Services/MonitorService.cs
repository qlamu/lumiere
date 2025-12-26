using Lumiere.Models;
using Lumiere.Native;
using static Lumiere.Native.User32Interop;

namespace Lumiere.Services;

public class MonitorService : IDisposable
{
    private readonly List<DisplayMonitor> _monitors = new();
    private readonly List<PHYSICAL_MONITOR> _physicalMonitors = new();
    private bool _disposed;
    private bool _isInitialized;

    public IReadOnlyList<DisplayMonitor> Monitors => _monitors;
    public bool IsInitialized => _isInitialized;

    public void Invalidate()
    {
        _isInitialized = false;
    }

    public event Action<DisplayMonitor, int>? BrightnessChanged;

    public void NotifyBrightnessChanged(DisplayMonitor monitor, int brightness)
    {
        BrightnessChanged?.Invoke(monitor, brightness);
    }

    public void RefreshMonitors(bool force = false)
    {
        if (_isInitialized && !force)
            return;

        CleanupMonitors();
        _monitors.Clear();
        _physicalMonitors.Clear();

        var monitorHandles = new List<IntPtr>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
        {
            monitorHandles.Add(hMonitor);
            return true;
        }, IntPtr.Zero);

        int index = 1;
        foreach (var hMonitor in monitorHandles)
        {
            var monitorInfo = MONITORINFOEX.Create();
            GetMonitorInfo(hMonitor, ref monitorInfo);

            if (!DxvaInterop.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count) || count == 0)
            {
                _monitors.Add(new DisplayMonitor
                {
                    DeviceName = monitorInfo.szDevice,
                    DisplayName = $"Display {index}",
                    PhysicalMonitorHandle = IntPtr.Zero,
                    HMonitor = hMonitor,
                    SupportsDdcCi = false,
                    ErrorMessage = "Monitor does not support DDC/CI"
                });
                index++;
                continue;
            }

            var physicalMonitors = new PHYSICAL_MONITOR[count];
            if (!DxvaInterop.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physicalMonitors))
            {
                _monitors.Add(new DisplayMonitor
                {
                    DeviceName = monitorInfo.szDevice,
                    DisplayName = $"Display {index}",
                    PhysicalMonitorHandle = IntPtr.Zero,
                    HMonitor = hMonitor,
                    SupportsDdcCi = false,
                    ErrorMessage = "Failed to get physical monitor"
                });
                index++;
                continue;
            }

            foreach (var physicalMonitor in physicalMonitors)
            {
                _physicalMonitors.Add(physicalMonitor);

                var displayName = string.IsNullOrWhiteSpace(physicalMonitor.szPhysicalMonitorDescription)
                    ? $"Display {index}"
                    : physicalMonitor.szPhysicalMonitorDescription;

                var monitor = new DisplayMonitor
                {
                    DeviceName = monitorInfo.szDevice,
                    DisplayName = displayName,
                    PhysicalMonitorHandle = physicalMonitor.hPhysicalMonitor,
                    HMonitor = hMonitor
                };

                // Try to get brightness
                if (DxvaInterop.GetMonitorBrightness(
                        physicalMonitor.hPhysicalMonitor,
                        out uint minBrightness,
                        out uint currentBrightness,
                        out uint maxBrightness))
                {
                    monitor.MinBrightness = (int)minBrightness;
                    monitor.MaxBrightness = (int)maxBrightness;
                    monitor.CurrentBrightness = (int)currentBrightness;
                    monitor.SupportsDdcCi = true;
                }
                else
                {
                    monitor.SupportsDdcCi = false;
                    monitor.ErrorMessage = "Failed to read brightness via DDC/CI";
                }

                _monitors.Add(monitor);
                index++;
            }
        }

        _isInitialized = true;
    }

    public bool SetBrightness(DisplayMonitor monitor, int brightness, bool notify = true)
    {
        if (!monitor.SupportsDdcCi || monitor.PhysicalMonitorHandle == IntPtr.Zero)
            return false;

        brightness = Math.Clamp(brightness, monitor.MinBrightness, monitor.MaxBrightness);

        if (DxvaInterop.SetMonitorBrightness(monitor.PhysicalMonitorHandle, (uint)brightness))
        {
            monitor.CurrentBrightness = brightness;
            if (notify)
                BrightnessChanged?.Invoke(monitor, brightness);
            return true;
        }

        return false;
    }

    public int? GetBrightness(DisplayMonitor monitor)
    {
        if (!monitor.SupportsDdcCi || monitor.PhysicalMonitorHandle == IntPtr.Zero)
            return null;

        if (DxvaInterop.GetMonitorBrightness(
                monitor.PhysicalMonitorHandle,
                out _,
                out uint currentBrightness,
                out _))
        {
            monitor.CurrentBrightness = (int)currentBrightness;
            return (int)currentBrightness;
        }

        return null;
    }

    public void AdjustAllBrightness(int delta)
    {
        foreach (var monitor in _monitors.Where(m => m.SupportsDdcCi))
        {
            var newBrightness = Math.Clamp(
                monitor.CurrentBrightness + delta,
                monitor.MinBrightness,
                monitor.MaxBrightness);
            SetBrightness(monitor, newBrightness);
        }
    }

    public void SetAllBrightness(int brightness)
    {
        foreach (var monitor in _monitors.Where(m => m.SupportsDdcCi))
        {
            SetBrightness(monitor, brightness);
        }
    }

    private void CleanupMonitors()
    {
        foreach (var pm in _physicalMonitors)
        {
            if (pm.hPhysicalMonitor != IntPtr.Zero)
            {
                DxvaInterop.DestroyPhysicalMonitor(pm.hPhysicalMonitor);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        CleanupMonitors();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~MonitorService()
    {
        Dispose();
    }
}
