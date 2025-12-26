# Lumiere

A lightweight Windows app to control monitor brightness via DDC/CI.

![Brightness popup](assets/popup.png)
![Settings window](assets/settings.png)

## Features

- **System tray app** - Lives in your taskbar, click to adjust brightness
- **Multi-monitor support** - Control each monitor independently
- **Smooth sliders** - Responsive UI with async DDC/CI updates
- **Mouse wheel support** - Scroll on slider to adjust brightness
- **Monitor hot-plug** - Automatically detects when monitors are connected/disconnected
- **Customizable keyboard shortcuts** (defaults):
  - `Ctrl+Alt+Up` - Increase brightness by 5%
  - `Ctrl+Alt+Down` - Decrease brightness by 5%
  - `Ctrl+Alt+D` - Day mode preset
  - `Ctrl+Alt+N` - Night mode preset
- **Day/Night presets** - Quickly switch between brightness levels
- **Launch at startup** - Optional auto-start with Windows
- **Windows 11 Mica** - Native look with translucent backdrop and system accent color

## Requirements

- Windows 10/11
- .NET 8 Desktop Runtime
- Monitor with DDC/CI support

## Building

```bash
cd src/Lumiere
dotnet build
```

## Publishing

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

## License

MIT
