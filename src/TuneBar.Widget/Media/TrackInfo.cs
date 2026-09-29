namespace TuneBar.Media;

public sealed record TrackInfo(
    string SourceAppId,
    string Title,
    string Artist,
    byte[]? Cover,
    bool IsPlaying)
{
    public bool IsSameSongAs(TrackInfo? other) =>
        other is not null
        && other.SourceAppId == SourceAppId
        && other.Title == Title
        && other.Artist == Artist;
}
