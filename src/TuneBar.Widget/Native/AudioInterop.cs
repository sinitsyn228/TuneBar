using System;
using System.Runtime.InteropServices;

namespace TuneBar.Native;

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class MMDeviceEnumerator
{
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
internal interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
internal interface IMMDevice
{
    [PreserveSig]
    int Activate(ref Guid interfaceId, int classContext, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
internal interface IAudioSessionManager2
{
    [PreserveSig]
    int GetAudioSessionControl(IntPtr sessionId, int streamFlags, out IntPtr sessionControl);

    [PreserveSig]
    int GetSimpleAudioVolume(IntPtr sessionId, int streamFlags, out IntPtr audioVolume);

    [PreserveSig]
    int GetSessionEnumerator(out IAudioSessionEnumerator enumerator);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
internal interface IAudioSessionEnumerator
{
    [PreserveSig]
    int GetCount(out int count);

    [PreserveSig]
    int GetSession(int index, out IAudioSessionControl2 session);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
internal interface IAudioSessionControl2
{
    [PreserveSig]
    int GetState(out int state);

    [PreserveSig]
    int GetDisplayName(out IntPtr displayName);

    [PreserveSig]
    int SetDisplayName(IntPtr displayName, ref Guid eventContext);

    [PreserveSig]
    int GetIconPath(out IntPtr iconPath);

    [PreserveSig]
    int SetIconPath(IntPtr iconPath, ref Guid eventContext);

    [PreserveSig]
    int GetGroupingParam(out Guid groupingParam);

    [PreserveSig]
    int SetGroupingParam(ref Guid groupingParam, ref Guid eventContext);

    [PreserveSig]
    int RegisterAudioSessionNotification(IntPtr notifications);

    [PreserveSig]
    int UnregisterAudioSessionNotification(IntPtr notifications);

    [PreserveSig]
    int GetSessionIdentifier(out IntPtr sessionIdentifier);

    [PreserveSig]
    int GetSessionInstanceIdentifier(out IntPtr sessionInstanceIdentifier);

    [PreserveSig]
    int GetProcessId(out uint processId);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
internal interface ISimpleAudioVolume
{
    [PreserveSig]
    int SetMasterVolume(float level, ref Guid eventContext);

    [PreserveSig]
    int GetMasterVolume(out float level);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
internal interface IAudioEndpointVolume
{
    [PreserveSig]
    int RegisterControlChangeNotify(IntPtr notify);

    [PreserveSig]
    int UnregisterControlChangeNotify(IntPtr notify);

    [PreserveSig]
    int GetChannelCount(out uint channelCount);

    [PreserveSig]
    int SetMasterVolumeLevel(float levelDecibels, ref Guid eventContext);

    [PreserveSig]
    int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);

    [PreserveSig]
    int GetMasterVolumeLevel(out float levelDecibels);

    [PreserveSig]
    int GetMasterVolumeLevelScalar(out float level);
}
