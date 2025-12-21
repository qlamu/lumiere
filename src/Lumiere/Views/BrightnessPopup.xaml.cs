using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Lumiere.Models;
using Lumiere.Native;
using Lumiere.Services;
using Forms = System.Windows.Forms;

namespace Lumiere.Views;

public partial class BrightnessPopup : Window
{
    private readonly MonitorService _monitorService;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _autoCloseTimer;
    private readonly Dictionary<string, Action<int>> _sliderUpdaters = new();
    private bool _isClosing;

    private const int AutoCloseSeconds = 5;

    // Cached brushes to reduce allocations
    private static readonly SolidColorBrush LabelBrush;
    private static readonly SolidColorBrush ErrorBrush;
    private static readonly SolidColorBrush TrackBgBrush;

    static BrightnessPopup()
    {
        // Freeze brushes for better performance
        LabelBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
        ErrorBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 100, 100));
        TrackBgBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255));
        LabelBrush.Freeze();
        ErrorBrush.Freeze();
        TrackBgBrush.Freeze();
    }

    #region Native Methods

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMSBT_MAINWINDOW = 2; // Mica
    private const int DWMSBT_TABBEDWINDOW = 4; // Mica Alt
    private const int DWMWCP_ROUND = 2; // Rounded corners

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int Left, Right, Top, Bottom;
        public MARGINS(int value) => Left = Right = Top = Bottom = value;
    }

    #endregion

    public BrightnessPopup(MonitorService monitorService, SettingsService settingsService)
    {
        _monitorService = monitorService;
        _settingsService = settingsService;

        InitializeComponent();
        CreateControls();

        // Auto-close timer
        _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(AutoCloseSeconds) };
        _autoCloseTimer.Tick += (s, e) => AnimateClose();

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
        PreviewMouseMove += (s, e) => ResetTimer();
        PreviewMouseDown += (s, e) => ResetTimer();

        _monitorService.BrightnessChanged += OnBrightnessChanged;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _autoCloseTimer.Stop();
        _monitorService.BrightnessChanged -= OnBrightnessChanged;
        _sliderUpdaters.Clear();
    }

    private void OnBrightnessChanged(DisplayMonitor monitor, int brightness)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            if (_sliderUpdaters.TryGetValue(monitor.DeviceName, out var updater))
            {
                updater(brightness);
                ResetTimer();
            }
        });
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        EnableMica();
    }

    private void EnableMica()
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        // Enable dark mode
        int darkMode = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        // Enable rounded corners
        int cornerPreference = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

        // Extend frame into client area (required for Mica)
        var margins = new MARGINS(-1);
        DwmExtendFrameIntoClientArea(hwnd, ref margins);

        // Enable Mica backdrop
        int micaValue = DWMSBT_TABBEDWINDOW;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref micaValue, sizeof(int));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionNearTray();
        AnimateOpen();
        _autoCloseTimer.Start();

        // Force window to be topmost (above taskbar)
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        // Activate window so Deactivated event fires properly
        Activate();
        Focus();
    }

    private void ResetTimer()
    {
        _autoCloseTimer.Stop();
        _autoCloseTimer.Start();
    }

    private void AnimateOpen()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(250);

        // Animate window position (whole window slides up)
        var slideUp = new DoubleAnimation(Top + 12, Top, duration) { EasingFunction = ease };
        BeginAnimation(TopProperty, slideUp);
    }

    private void AnimateClose()
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoCloseTimer.Stop();

        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(150);

        var slideDown = new DoubleAnimation(Top, Top + 8, duration) { EasingFunction = ease };
        slideDown.Completed += (s, e) => Close();

        BeginAnimation(TopProperty, slideDown);
    }

    private void CreateControls()
    {
        _monitorService.RefreshMonitors();

        var monitors = _monitorService.Monitors.ToList();
        for (int i = 0; i < monitors.Count; i++)
        {
            bool isLast = i == monitors.Count - 1;
            var panel = CreateMonitorControl(monitors[i], isLast);
            MonitorPanel.Children.Add(panel);
        }
    }

    private StackPanel CreateMonitorControl(DisplayMonitor monitor, bool isLast)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, isLast ? 0 : 12) };

        var label = new TextBlock
        {
            Text = monitor.DisplayName,
            Foreground = LabelBrush,
            FontSize = 12,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        panel.Children.Add(label);

        if (!monitor.SupportsDdcCi)
        {
            var errorLabel = new TextBlock
            {
                Text = monitor.ErrorMessage ?? "DDC/CI not supported",
                Foreground = ErrorBrush,
                FontSize = 11
            };
            panel.Children.Add(errorLabel);
            return panel;
        }

        var sliderRow = new Grid();
        sliderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sliderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var sliderContainer = CreateModernSlider(monitor);
        Grid.SetColumn(sliderContainer, 0);
        sliderRow.Children.Add(sliderContainer);

        var valueLabel = new TextBlock
        {
            Text = $"{monitor.CurrentBrightness}%",
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 12,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            Width = 36,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(valueLabel, 1);
        sliderRow.Children.Add(valueLabel);

        panel.Children.Add(sliderRow);
        return panel;
    }

    private Grid CreateModernSlider(DisplayMonitor monitor)
    {
        var container = new Grid { Height = 20, Margin = new Thickness(0, 2, 0, 0) };

        var trackBg = new Border
        {
            Height = 4,
            Background = TrackBgBrush,
            CornerRadius = new CornerRadius(2),
            VerticalAlignment = VerticalAlignment.Center
        };
        container.Children.Add(trackBg);

        var trackFill = new Border
        {
            Height = 4,
            Background = AccentColorHelper.AccentBrush,
            CornerRadius = new CornerRadius(2),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left
        };
        container.Children.Add(trackFill);

        var thumb = new Ellipse
        {
            Width = 16,
            Height = 16,
            Fill = AccentColorHelper.AccentBrush,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        container.Children.Add(thumb);

        var thumbInner = new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = System.Windows.Media.Brushes.White,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        container.Children.Add(thumbInner);

        TextBlock? valueLabel = null;
        double min = monitor.MinBrightness;
        double max = monitor.MaxBrightness;
        double value = monitor.CurrentBrightness;
        bool isDragging = false;

        void UpdateVisuals()
        {
            var width = container.ActualWidth;
            if (width <= 0) return;

            var ratio = (value - min) / (max - min);
            var fillWidth = ratio * width;
            var thumbX = ratio * (width - 16);

            trackFill.Width = Math.Max(0, fillWidth);
            thumb.Margin = new Thickness(thumbX, 0, 0, 0);
            thumbInner.Margin = new Thickness(thumbX + 5, 0, 0, 0);

            if (valueLabel == null)
            {
                var parent = container.Parent as Grid;
                if (parent != null && parent.Children.Count > 1)
                    valueLabel = parent.Children[1] as TextBlock;
            }
            if (valueLabel != null)
                valueLabel.Text = $"{(int)value}%";
        }

        container.SizeChanged += (s, e) => UpdateVisuals();
        container.Loaded += (s, e) => UpdateVisuals();

        thumb.MouseLeftButtonDown += (s, e) =>
        {
            isDragging = true;
            thumb.CaptureMouse();
            e.Handled = true;
        };

        thumb.MouseLeftButtonUp += (s, e) =>
        {
            if (isDragging)
            {
                isDragging = false;
                thumb.ReleaseMouseCapture();
                _settingsService.SaveLastBrightness(monitor.DeviceName, (int)value);
            }
        };

        thumb.MouseMove += (s, e) =>
        {
            if (!isDragging) return;
            var pos = e.GetPosition(container);
            var width = container.ActualWidth;
            var ratio = Math.Clamp(pos.X / width, 0, 1);
            value = Math.Round(min + ratio * (max - min));
            _monitorService.SetBrightness(monitor, (int)value);
            UpdateVisuals();
        };

        container.MouseLeftButtonDown += (s, e) =>
        {
            var pos = e.GetPosition(container);
            var width = container.ActualWidth;
            var ratio = Math.Clamp(pos.X / width, 0, 1);
            value = Math.Round(min + ratio * (max - min));
            _monitorService.SetBrightness(monitor, (int)value);
            _settingsService.SaveLastBrightness(monitor.DeviceName, (int)value);
            UpdateVisuals();
        };

        // Register updater for external brightness changes
        _sliderUpdaters[monitor.DeviceName] = newBrightness =>
        {
            value = newBrightness;
            UpdateVisuals();
        };

        return container;
    }

    private void PositionNearTray()
    {
        var source = PresentationSource.FromVisual(this);
        double dpi = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

        var screen = Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens[0];
        var workArea = screen.WorkingArea;
        var bounds = screen.Bounds;

        double w = ActualWidth * dpi;
        double h = ActualHeight * dpi;
        double marginX = 12 * dpi;

        // Detect auto-hide taskbar (work area equals screen bounds)
        // Add taskbar height (~48px) + margin (12px) when auto-hide
        bool isAutoHideTaskbar = workArea.Bottom == bounds.Bottom;
        double marginY = isAutoHideTaskbar ? 60 * dpi : 12 * dpi;

        // Position: bottom-right corner with margins
        double left = workArea.Right - w - marginX;
        double top = workArea.Bottom - h - marginY;

        Left = left / dpi;
        Top = top / dpi;
    }

    private void Window_Deactivated(object sender, EventArgs e) => AnimateClose();
}
