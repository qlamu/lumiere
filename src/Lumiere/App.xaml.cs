using System.Drawing;
using System.Windows;
using Lumiere.Services;
using Lumiere.ViewModels;
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

        // Initialize view model
        _viewModel = new MainViewModel(_monitorService, _settingsService, _hotkeyService);

        // Setup native tray icon
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Lumiere",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _trayIcon.Click += OnTrayIconClick;

        // Initialize hotkeys
        _hotkeyService.Initialize(_viewModel);
    }

    private void OnTrayIconClick(object? sender, EventArgs e)
    {
        if (e is Forms.MouseEventArgs mouseArgs && mouseArgs.Button == Forms.MouseButtons.Left)
        {
            _viewModel?.ShowPopupCommand.Execute(null);
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

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private static Icon LoadIcon()
    {
        // Load from embedded resource
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Lumiere.Resources.icon.ico");
        if (stream != null)
        {
            return new Icon(stream);
        }

        // Fallback: create a simple icon programmatically
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.White);
        g.FillEllipse(brush, 8, 8, 16, 16);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    protected override void OnExit(ExitEventArgs e)
    {
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
}
