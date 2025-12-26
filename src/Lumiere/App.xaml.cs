using System.Runtime.InteropServices;
using System.Windows;
using Lumiere.Native;
using Lumiere.Services;
using Lumiere.ViewModels;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Application = System.Windows.Application;

namespace Lumiere;

public partial class App : Application
{
    private static Mutex? _mutex;
    private Forms.NotifyIcon? _trayIcon;
    private MainViewModel? _viewModel;
    private MonitorService? _monitorService;
    private HotkeyService? _hotkeyService;
    private SettingsService? _settingsService;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance check
        const string mutexName = "Lumiere_SingleInstance_Mutex";
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            System.Windows.MessageBox.Show("Lumiere is already running.", "Lumiere", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Initialize services
        _settingsService = new SettingsService();
        _monitorService = new MonitorService();
        _hotkeyService = new HotkeyService();

        // Pre-initialize monitors so first popup works immediately
        _monitorService.RefreshMonitors();

        // Initialize view model
        _viewModel = new MainViewModel(_monitorService, _settingsService, _hotkeyService);

        // Setup native tray icon
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = TrayIconHelper.CreateSunIcon(),
            Text = "Lumiere",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _trayIcon.Click += OnTrayIconClick;

        // Listen for theme changes to update tray icon
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        // Initialize hotkeys
        _hotkeyService.Initialize(_viewModel, _settingsService, _monitorService);

        // Reduce memory footprint after startup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        TrimWorkingSet();
    }

    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minSize, IntPtr maxSize);

    private static void TrimWorkingSet()
    {
        SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
    }

    private void OnTrayIconClick(object? sender, EventArgs e)
    {
        if (e is Forms.MouseEventArgs mouseArgs && mouseArgs.Button == Forms.MouseButtons.Left)
        {
            _viewModel?.ShowPopupCommand.Execute(null);
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && _trayIcon != null)
        {
            var oldIcon = _trayIcon.Icon;
            _trayIcon.Icon = TrayIconHelper.CreateSunIcon();
            oldIcon?.Dispose();
        }
    }

    private Forms.ContextMenuStrip CreateContextMenu()
    {
        var menu = new Forms.ContextMenuStrip();

        var showItem = new Forms.ToolStripMenuItem("Show");
        showItem.Click += (s, e) => _viewModel?.ShowPopupCommand.Execute(null);
        menu.Items.Add(showItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var dayItem = new Forms.ToolStripMenuItem("Day Mode");
        dayItem.Click += (s, e) => _viewModel?.ApplyPresetByName("Day");
        menu.Items.Add(dayItem);

        var nightItem = new Forms.ToolStripMenuItem("Night Mode");
        nightItem.Click += (s, e) => _viewModel?.ApplyPresetByName("Night");
        menu.Items.Add(nightItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var settingsItem = new Forms.ToolStripMenuItem("Settings...");
        settingsItem.Click += (s, e) => _viewModel?.ShowSettingsCommand.Execute(null);
        menu.Items.Add(settingsItem);

        var startupItem = new Forms.ToolStripMenuItem("Launch at startup");
        startupItem.Checked = IsStartupEnabled();
        startupItem.Click += (s, e) =>
        {
            startupItem.Checked = !startupItem.Checked;
            SetStartupEnabled(startupItem.Checked);
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _hotkeyService?.Dispose();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    #region Startup Registry

    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Lumiere";

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
        return key?.GetValue(AppName) != null;
    }

    private static void SetStartupEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Environment.ProcessPath;
            if (exePath != null)
            {
                key.SetValue(AppName, $"\"{exePath}\"");
            }
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }

    #endregion
}
