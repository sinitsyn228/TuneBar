using System;
using System.Collections.Generic;
using System.Windows.Interop;
using TuneBar.Native;

namespace TuneBar.Platform;

public sealed class GlobalHotkeys : IDisposable
{
    public const uint ModControl = 0x0002;
    public const uint KeySpace = 0x20;
    public const uint KeyLeft = 0x25;
    public const uint KeyUp = 0x26;
    public const uint KeyRight = 0x27;
    public const uint KeyDown = 0x28;

    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;

    private static readonly IntPtr MessageOnlyParent = new(-3);

    private readonly HwndSource source;
    private readonly Dictionary<int, Action> actions = new();

    public GlobalHotkeys()
    {
        source = new HwndSource(new HwndSourceParameters("TuneBar.Hotkeys")
        {
            ParentWindow = MessageOnlyParent,
            WindowStyle = 0,
        });
        source.AddHook(OnWindowMessage);
    }

    public void Register(uint modifiers, uint key, Action action, bool repeatWhileHeld = false)
    {
        var id = actions.Count + 1;
        var flags = repeatWhileHeld ? modifiers : modifiers | ModNoRepeat;
        if (NativeMethods.RegisterHotKey(source.Handle, id, flags, key))
            actions[id] = action;
    }

    public void Dispose()
    {
        foreach (var id in actions.Keys)
            NativeMethods.UnregisterHotKey(source.Handle, id);

        actions.Clear();
        source.RemoveHook(OnWindowMessage);
        source.Dispose();
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
