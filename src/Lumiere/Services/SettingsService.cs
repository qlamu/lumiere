using System.IO;
using System.Text.Json;
using Lumiere.Models;

namespace Lumiere.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppSettings Settings { get; private set; }

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var lumiereFolder = Path.Combine(appDataPath, "Lumiere");
        Directory.CreateDirectory(lumiereFolder);
        _settingsPath = Path.Combine(lumiereFolder, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Settings = Load();
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (settings != null)
                {
                    Settings = settings;
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }

        Settings = new AppSettings();
        return Settings;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public void SaveLastBrightness(string monitorId, int brightness)
    {
        Settings.LastBrightnessValues[monitorId] = brightness;
        Save();
    }

    public int? GetLastBrightness(string monitorId)
    {
        return Settings.LastBrightnessValues.TryGetValue(monitorId, out var brightness)
            ? brightness
            : null;
    }

    public BrightnessPreset? GetPreset(string name)
    {
        return Settings.Presets.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void SavePreset(BrightnessPreset preset)
    {
        var existing = Settings.Presets.FirstOrDefault(p =>
            p.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.MonitorBrightnessLevels = preset.MonitorBrightnessLevels;
        }
        else
        {
            Settings.Presets.Add(preset);
        }

        Save();
    }
}
