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

    public MainViewModel(MonitorService monitorService, SettingsService settingsService, HotkeyService hotkeyService)
    {
        _monitorService = monitorService;
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
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
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    public void AdjustBrightness(int delta)
    {
        _monitorService.RefreshMonitors();
        foreach (var monitor in _monitorService.Monitors.Where(m => m.SupportsDdcCi))
        {
            var newBrightness = Math.Clamp(
                monitor.CurrentBrightness + delta,
                monitor.MinBrightness,
                monitor.MaxBrightness);
            _monitorService.SetBrightness(monitor, newBrightness);
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
