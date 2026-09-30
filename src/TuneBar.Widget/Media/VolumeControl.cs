using System;
using System.Diagnostics;
using TuneBar.Native;

namespace TuneBar.Media;

public static class VolumeControl
{
    private const float Step = 0.05f;
    private const int RenderDataFlow = 0;
    private const int MultimediaRole = 1;
    private const int AllClassContexts = 23;
    private const string FirefoxAppId = "308046B0AF4A39CB";

    public static void Raise(string? appId) => Change(appId, Step);

    public static void Lower(string? appId) => Change(appId, -Step);

    private static void Change(string? appId, float delta)
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            if (enumerator.GetDefaultAudioEndpoint(RenderDataFlow, MultimediaRole, out var device) != 0)
                return;

            if (!ChangeAppVolume(device, appId, delta))
                ChangeDeviceVolume(device, delta);
        }
        catch (Exception)
        {
        }
    }

    private static bool ChangeAppVolume(IMMDevice device, string? appId, float delta)
    {
        if (string.IsNullOrEmpty(appId))
            return false;

        var sessionManager = Activate<IAudioSessionManager2>(device);
        if (sessionManager is null || sessionManager.GetSessionEnumerator(out var sessions) != 0)
            return false;

        sessions.GetCount(out var count);
        var changed = false;

        for (var i = 0; i < count; i++)
        {
            if (sessions.GetSession(i, out var session) != 0 || !BelongsToApp(session, appId))
                continue;

            var volume = (ISimpleAudioVolume)session;
            var context = Guid.Empty;
            if (volume.GetMasterVolume(out var level) == 0 && volume.SetMasterVolume(Math.Clamp(level + delta, 0f, 1f), ref context) == 0)
                changed = true;
        }

        return changed;
    }

    private static void ChangeDeviceVolume(IMMDevice device, float delta)
    {
        var volume = Activate<IAudioEndpointVolume>(device);
        if (volume is null || volume.GetMasterVolumeLevelScalar(out var level) != 0)
            return;

        var context = Guid.Empty;
        volume.SetMasterVolumeLevelScalar(Math.Clamp(level + delta, 0f, 1f), ref context);
    }

    private static T? Activate<T>(IMMDevice device)
        where T : class
    {
        var interfaceId = typeof(T).GUID;
        return device.Activate(ref interfaceId, AllClassContexts, IntPtr.Zero, out var instance) == 0 ? instance as T : null;
    }

    private static bool BelongsToApp(IAudioSessionControl2 session, string appId)
    {
        if (session.GetProcessId(out var processId) != 0 || processId == 0)
            return false;

        var processName = GetProcessName(processId);
        if (string.IsNullOrEmpty(processName))
            return false;

        return appId.Contains(processName, StringComparison.OrdinalIgnoreCase)
            || (appId.Contains(FirefoxAppId, StringComparison.OrdinalIgnoreCase) && processName.Equals("firefox", StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
