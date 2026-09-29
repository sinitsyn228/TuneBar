using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using TuneBar.Native;

namespace TuneBar.Platform;

public sealed class GlobalHotkeys : IDisposable
{
    public const uint ModControl = 0x0002;
    public const uint KeyLeft = 0x25;
    public const uint KeyRight = 0x27;

    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;

    private readonly IntPtr handle;
    private readonly HwndSource source;
    private readonly Dictionary<int, Action> actions = new();

    public GlobalHotkeys(Window window)
    {
        handle = new WindowInteropHelper(window).Handle;
        source = HwndSource.FromHwnd(handle);
        source.AddHook(OnWindowMessage);
    }

    public void Register(uint modifiers, uint key, Action action)
    {
        var id = actions.Count + 1;
        if (NativeMethods.RegisterHotKey(handle, id, modifiers | ModNoRepeat, key))
            actions[id] = action;
    }

    public void Dispose()
    {
        foreach (var id in actions.Keys)
            NativeMethods.UnregisterHotKey(handle, id);

        actions.Clear();
        source.RemoveHook(OnWindowMessage);
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }

        return IntPtr.Zero;
    }
}
