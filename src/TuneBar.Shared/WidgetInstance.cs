using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TuneBar.Shared;

public static class WidgetInstance
{
    public const string MutexName = "TuneBar.Widget.SingleInstance";
    public const string ExitEventName = "TuneBar.Widget.Exit";

    private static readonly TimeSpan StateChangeTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    public static string ExecutablePath => Path.Combine(AppContext.BaseDirectory, "TuneBar.Widget.exe");

    public static string LauncherPath => Path.Combine(AppContext.BaseDirectory, "TuneBar.exe");

    public static bool IsInstalled => File.Exists(ExecutablePath);

    public static bool IsRunning
    {
        get
        {
            if (!Mutex.TryOpenExisting(MutexName, out var mutex))
                return false;

            mutex.Dispose();
            return true;
        }
    }

    public static bool Start()
    {
        if (IsRunning)
            return true;

        Process.Start(new ProcessStartInfo(ExecutablePath) { UseShellExecute = false });
        return WaitUntil(() => IsRunning);
    }

    public static bool Stop()
    {
        if (!EventWaitHandle.TryOpenExisting(ExitEventName, out var exitEvent))
            return !IsRunning;

        using (exitEvent)
            exitEvent.Set();

        return WaitUntil(() => !IsRunning);
    }

    public static bool Restart() => Stop() && Start();

    private static bool WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + StateChangeTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;
            Thread.Sleep(PollInterval);
        }

        return condition();
    }
}
