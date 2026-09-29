using System;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TuneBar.Native;
using TuneBar.Shared;

namespace TuneBar.Taskbar;

public sealed class TaskbarDock
{
    private readonly Window window;
    private readonly DispatcherTimer timer;
    private WidgetSettings settings = WidgetSettings.Load();
    private DateTime settingsSavedUtc = WidgetSettings.LastSavedUtc;
    private IntPtr handle;

    public TaskbarDock(Window window)
    {
        this.window = window;
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => Reposition();
    }

    public bool IsSuppressed { get; private set; }

    public WidgetSettings Settings => settings;

    public event Action? SuppressedChanged;

    public event Action? SettingsChanged;

    public void Start()
    {
        handle = new WindowInteropHelper(window).Handle;
        MakeToolWindow();
        Reposition();
        timer.Start();
    }

    private void MakeToolWindow()
    {
        var style = (long)NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle);
        style |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(style));
    }

    private void ReloadSettingsIfChanged()
    {
        var savedUtc = WidgetSettings.LastSavedUtc;
        if (savedUtc == settingsSavedUtc)
            return;

        settingsSavedUtc = savedUtc;
        settings = WidgetSettings.Load();
        SettingsChanged?.Invoke();
    }

    private void Reposition()
    {
        ReloadSettingsIfChanged();

        var taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
        var suppressed = taskbar == IntPtr.Zero
            || !NativeMethods.GetWindowRect(taskbar, out var taskbarRect)
            || taskbarRect.Width < taskbarRect.Height
            || IsForegroundFullscreen();

        SetSuppressed(suppressed);
        if (suppressed)
            return;

        NativeMethods.GetWindowRect(taskbar, out taskbarRect);
        var dpi = VisualTreeHelper.GetDpi(window);
        window.Height = taskbarRect.Height / dpi.DpiScaleY;
        var widthPixels = (int)Math.Round(window.ActualWidth * dpi.DpiScaleX);

        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HwndTopmost,
            CalculateLeft(taskbar, taskbarRect, widthPixels),
            taskbarRect.Top,
            0,
            0,
            NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);
    }

    private int CalculateLeft(IntPtr taskbar, NativeMethods.Rect taskbarRect, int widthPixels)
    {
        var fromLeft = taskbarRect.Left + settings.OffsetPixels;

        var anchorClass = settings.Position switch
        {
            WidgetPosition.BeforeIcons => "ReBarWindow32",
            WidgetPosition.Right => "TrayNotifyWnd",
            _ => null,
        };
        if (anchorClass is null)
            return fromLeft;

        var anchor = NativeMethods.FindWindowEx(taskbar, IntPtr.Zero, anchorClass, null);
        if (anchor == IntPtr.Zero || !NativeMethods.GetWindowRect(anchor, out var anchorRect))
            return fromLeft;

        var startButtonWidth = settings.Position == WidgetPosition.BeforeIcons ? taskbarRect.Height : 0;
        return Math.Max(taskbarRect.Left, anchorRect.Left - startButtonWidth - widthPixels - settings.OffsetPixels);
    }

    private void SetSuppressed(bool suppressed)
    {
        if (IsSuppressed == suppressed)
            return;

        IsSuppressed = suppressed;
        SuppressedChanged?.Invoke();
    }

    private static bool IsForegroundFullscreen()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == NativeMethods.GetShellWindow() || IsDesktopWindow(foreground))
            return false;

        if (!NativeMethods.GetWindowRect(foreground, out var windowRect))
            return false;

        var monitor = NativeMethods.MonitorFromWindow(foreground, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
            return false;

        return windowRect.Left <= info.Monitor.Left
            && windowRect.Top <= info.Monitor.Top
            && windowRect.Right >= info.Monitor.Right
            && windowRect.Bottom >= info.Monitor.Bottom;
    }

    private static bool IsDesktopWindow(IntPtr hwnd)
    {
        var className = new StringBuilder(64);
        NativeMethods.GetClassName(hwnd, className, className.Capacity);
        var name = className.ToString();
        return name is "WorkerW" or "Progman" or "Shell_TrayWnd";
    }
}
