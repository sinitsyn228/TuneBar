using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Control;

namespace TuneBar.Media;

public static class SessionPriority
{
    private static readonly string[] BrowserAppIds =
    [
        "308046B0AF4A39CB",
        "firefox",
        "chrome",
        "msedge",
        "opera",
        "brave",
        "vivaldi",
        "browser",
    ];

    public static GlobalSystemMediaTransportControlsSession? ChooseBest(
        IEnumerable<GlobalSystemMediaTransportControlsSession> sessions,
        GlobalSystemMediaTransportControlsSession? systemCurrent)
    {
        var currentAppId = systemCurrent?.SourceAppUserModelId;

        return sessions
            .OrderByDescending(IsMusicApp)
            .ThenByDescending(IsPlaying)
            .ThenByDescending(candidate => candidate.SourceAppUserModelId == currentAppId)
            .FirstOrDefault();
    }

    private static bool IsMusicApp(GlobalSystemMediaTransportControlsSession session) =>
        !BrowserAppIds.Any(id => session.SourceAppUserModelId.Contains(id, StringComparison.OrdinalIgnoreCase));

    private static bool IsPlaying(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            return session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
