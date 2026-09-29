using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace TuneBar.Media;

public sealed class MediaSessionService
{
    private readonly object sessionsLock = new();
    private GlobalSystemMediaTransportControlsSessionManager? manager;
    private List<GlobalSystemMediaTransportControlsSession> sessions = new();
    private GlobalSystemMediaTransportControlsSession? session;
    private TrackInfo? lastTrack;

    public event Action<TrackInfo?>? TrackChanged;

    public async Task StartAsync()
    {
        manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        manager.SessionsChanged += (_, _) => SubscribeToSessions();
        manager.CurrentSessionChanged += (_, _) => SelectSession();
        SubscribeToSessions();
    }

    public async Task PreviousAsync()
    {
        if (session is not null)
            await session.TrySkipPreviousAsync();
    }

    public async Task NextAsync()
    {
        if (session is not null)
            await session.TrySkipNextAsync();
    }

    public async Task TogglePlayPauseAsync()
    {
        if (session is not null)
            await session.TryTogglePlayPauseAsync();
    }

    private void SubscribeToSessions()
    {
        if (manager is null)
            return;

        lock (sessionsLock)
        {
            foreach (var existing in sessions)
            {
                existing.MediaPropertiesChanged -= OnSessionChanged;
                existing.PlaybackInfoChanged -= OnSessionChanged;
            }

            sessions = manager.GetSessions().ToList();

            foreach (var added in sessions)
            {
                added.MediaPropertiesChanged += OnSessionChanged;
                added.PlaybackInfoChanged += OnSessionChanged;
            }
        }

        SelectSession();
    }

    private void OnSessionChanged(GlobalSystemMediaTransportControlsSession sender, object args) => SelectSession();

    private void SelectSession()
    {
        lock (sessionsLock)
            session = SessionPriority.ChooseBest(sessions, manager?.GetCurrentSession());

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var current = session;
        if (current is null)
        {
            Publish(null);
            return;
        }

        try
        {
            var properties = await current.TryGetMediaPropertiesAsync();
            var playback = current.GetPlaybackInfo();
            var isPlaying = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            var track = new TrackInfo(current.SourceAppUserModelId, properties.Title, properties.Artist, null, isPlaying);

            var cover = track.IsSameSongAs(lastTrack) && lastTrack!.Cover is not null
                ? lastTrack.Cover
                : await ReadCoverAsync(properties.Thumbnail);

            if (!ReferenceEquals(current, session))
                return;

            Publish(track with { Cover = cover });
        }
        catch (Exception)
        {
        }
    }

    private void Publish(TrackInfo? track)
    {
        lastTrack = track;
        TrackChanged?.Invoke(track);
    }

    private static async Task<byte[]?> ReadCoverAsync(IRandomAccessStreamReference? reference)
    {
        if (reference is null)
            return null;

        using var stream = await reference.OpenReadAsync();
        using var source = stream.AsStreamForRead();
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
