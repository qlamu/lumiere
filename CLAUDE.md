# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Lumiere is a Windows system tray application for controlling monitor brightness via DDC-CI. It's a .NET 8 WPF desktop app with Windows 11 Mica styling.

## Build Commands

```bash
# Development build
cd src/Lumiere
dotnet build

# Run the application
dotnet run

# Release build (self-contained single executable)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Architecture

### MVVM with Service Layer
- **ViewModels/MainViewModel.cs**: Central coordinator for UI and business logic, uses CommunityToolkit.Mvvm
- **Services/**: Three core services encapsulate all business logic
  - `MonitorService`: DDC-CI communication, monitor enumeration, brightness control
  - `HotkeyService`: Global Windows hotkey registration via hidden window message pump
  - `SettingsService`: JSON persistence to `%APPDATA%\Lumiere\settings.json`
- **Views/**: WPF windows with Windows 11 Mica backdrop styling
  - `BrightnessPopup`: Main brightness control popup (auto-closes after 5 seconds)
  - `SettingsWindow`: Configuration for presets and hotkeys

### Native Interop (Native/)
Extensive P/Invoke for Windows APIs:
- **DxvaInterop.cs**: DXVA2.dll for DDC-CI brightness control
- **User32Interop.cs**: User32.dll for hotkeys and monitor enumeration
- **NativeStructs.cs**: Native structures and DWM constants for Mica styling

### Key Patterns
- **Single Instance**: Mutex-based prevention of multiple app instances (App.xaml.cs)
- **Throttling**: DispatcherTimer throttles DDC-CI commands (50ms) to avoid hardware saturation - immediate UI update, delayed hardware sync
- **Presets**: Support per-monitor brightness levels with wildcard "*" for defaults

## Entry Points

- **App.xaml.cs**: Application startup, singleton check, service initialization, system tray setup
- **MainViewModel.cs**: Commands (ShowPopup, ShowSettings), AdjustBrightness(), ApplyPresetByName()

## DDC-CI Notes

- Not all monitors support DDC-CI; service handles unsupported monitors gracefully
- Brightness range varies per monitor (min/max detection at runtime)
- Per-monitor physical handles (IntPtr) for independent control
