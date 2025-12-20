using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumiere.Models;
using Lumiere.Services;
using Lumiere.Views;

namespace Lumiere.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly MonitorService _monitorService;
    private readonly SettingsService _settingsService;
    private readonly HotkeyService _hotkeyService;

    private BrightnessPopup? _popup;
    private readonly DispatcherTimer _brightnessThrottle;
    private bool _monitorsInitialized;

    public MainViewModel(MonitorService monitorService, SettingsService settingsService, HotkeyService hotkeyService)
    {
        _monitorService = monitorService;
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;

        // Throttle brightness changes to avoid DDC/CI bottleneck
        _brightnessThrottle = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _brightnessThrottle.Tick += ApplyPendingBrightness;
    }

    [RelayCommand]
    private void ShowPopup()
    {
        if (_popup != null)
        {
            _popup.Close();
            _popup = null;
            return;
        }

        _popup = new BrightnessPopup(_monitorService, _settingsService);
        _popup.Closed += (s, e) => _popup = null;
        _popup.Show();
    }

    [RelayCommand]
    private void ShowSettings()
    {
        var settingsWindow = new SettingsWindow(_settingsService);
        settingsWindow.ShowDialog();
    }

    public void AdjustBrightness(int delta)
    {
        // Initialize monitors once
        if (!_monitorsInitialized)
        {
            _monitorService.RefreshMonitors();
            _monitorsInitialized = true;
        }

        // Update target brightness immediately for responsive UI
        foreach (var monitor in _monitorService.Monitors.Where(m => m.SupportsDdcCi))
        {
            var newBrightness = Math.Clamp(
                monitor.CurrentBrightness + delta,
                monitor.MinBrightness,
                monitor.MaxBrightness);
            monitor.CurrentBrightness = newBrightness;
            _monitorService.NotifyBrightnessChanged(monitor, newBrightness);
        }

        // Throttle actual DDC/CI calls
        _brightnessThrottle.Stop();
        _brightnessThrottle.Start();
    }

    private void ApplyPendingBrightness(object? sender, EventArgs e)
    {
        _brightnessThrottle.Stop();

        // Apply current brightness values to hardware (no notification, already done)
        foreach (var monitor in _monitorService.Monitors.Where(m => m.SupportsDdcCi))
        {
            _monitorService.SetBrightness(monitor, monitor.CurrentBrightness, notify: false);
        }
    }

    public void ApplyPresetByName(string presetName)
    {
        var preset = _settingsService.GetPreset(presetName);
        if (preset == null) return;

        _monitorService.RefreshMonitors();
        foreach (var monitor in _monitorService.Monitors.Where(m => m.SupportsDdcCi))
        {
            int brightness;
            if (preset.MonitorBrightnessLevels.TryGetValue(monitor.DeviceName, out var specific))
            {
                brightness = specific;
            }
            else if (preset.MonitorBrightnessLevels.TryGetValue("*", out var defaultVal))
            {
                brightness = defaultVal;
            }
            else
            {
                continue;
            }

            _monitorService.SetBrightness(monitor, brightness);
        }
    }
}
