using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TuneBar.Native;
using TuneBar.Shared;

namespace TuneBar.Taskbar;

public sealed class TaskbarDock
{
    private const string PrimaryTaskbarClass = "Shell_TrayWnd";
    private const string SecondaryTaskbarClass = "Shell_SecondaryTrayWnd";

    private readonly Window window;
    private readonly IntPtr taskbar;
    private IntPtr handle;

    public TaskbarDock(Window window, IntPtr taskbar)
    {
        this.window = window;
        this.taskbar = taskbar;
    }

    public bool IsSuppressed { get; private set; }

    public event Action? SuppressedChanged;

    public static IReadOnlyList<IntPtr> FindTaskbars(WidgetMonitor monitor)
    {
        var primary = FindTopLevelWindows(PrimaryTaskbarClass);
        var secondary = FindTopLevelWindows(SecondaryTaskbarClass);

        if (monitor == WidgetMonitor.All)
        {
            primary.AddRange(secondary);
            return primary;
        }

        return monitor == WidgetMonitor.Secondary && secondary.Count > 0 ? secondary : primary;
    }

    public void Attach()
    {
        handle = new WindowInteropHelper(window).Handle;
        MakeToolWindow();
    }

    public void Reposition(WidgetSettings settings)
    {
        if (handle == IntPtr.Zero)
            return;

        var suppressed = !NativeMethods.GetWindowRect(taskbar, out var taskbarRect)
            || taskbarRect.Width < taskbarRect.Height
            || IsCoveredByFullscreenWindow();

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
            CalculateLeft(settings, taskbarRect, widthPixels),
            taskbarRect.Top,
            0,
            0,
            NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);
    }

    private static List<IntPtr> FindTopLevelWindows(string className)
    {
        var windows = new List<IntPtr>();
        var current = IntPtr.Zero;
        while ((current = NativeMethods.FindWindowEx(IntPtr.Zero, current, className, null)) != IntPtr.Zero)
            windows.Add(current);

        return windows;
    }

    private void MakeToolWindow()
    {
        var style = (long)NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle);
        style |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(style));
    }

    private int CalculateLeft(WidgetSettings settings, NativeMethods.Rect taskbarRect, int widthPixels)
    {
        var fromLeft = taskbarRect.Left + settings.OffsetPixels;

        switch (settings.Position)
        {
            case WidgetPosition.BeforeIcons:
                if (!TryGetChildRect(out var iconsRect, "ReBarWindow32", "WorkerW"))
                    return fromLeft;

                var startButtonWidth = taskbarRect.Height;
                return Math.Max(taskbarRect.Left, iconsRect.Left - startButtonWidth - widthPixels - settings.OffsetPixels);

            case WidgetPosition.Right:
                var rightEdge = TryGetChildRect(out var trayRect, "TrayNotifyWnd") ? trayRect.Left : taskbarRect.Right;
                return Math.Max(taskbarRect.Left, rightEdge - widthPixels - settings.OffsetPixels);

            default:
                return fromLeft;
        }
    }

    private bool TryGetChildRect(out NativeMethods.Rect rect, params string[] classNames)
    {
        foreach (var className in classNames)
        {
            var child = NativeMethods.FindWindowEx(taskbar, IntPtr.Zero, className, null);
            if (child != IntPtr.Zero && NativeMethods.GetWindowRect(child, out rect))
                return true;
        }

        rect = default;
        return false;
    }

    private void SetSuppressed(bool suppressed)
    {
        if (IsSuppressed == suppressed)
            return;

        IsSuppressed = suppressed;
        SuppressedChanged?.Invoke();
    }

    private bool IsCoveredByFullscreenWindow()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == NativeMethods.GetShellWindow() || IsDesktopWindow(foreground))
            return false;

        if (!NativeMethods.GetWindowRect(foreground, out var windowRect))
            return false;

        var monitor = NativeMethods.MonitorFromWindow(foreground, NativeMethods.MonitorDefaultToNearest);
        if (monitor != NativeMethods.MonitorFromWindow(taskbar, NativeMethods.MonitorDefaultToNearest))
            return false;

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
        return name is "WorkerW" or "Progman" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";
    }
}
