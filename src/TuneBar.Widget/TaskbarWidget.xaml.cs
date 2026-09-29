using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TuneBar.Media;
using TuneBar.Platform;
using TuneBar.Shared;
using TuneBar.Taskbar;

namespace TuneBar;

public partial class TaskbarWidget : Window
{
    private const string PlayGlyph = "";
    private const string PauseGlyph = "";

    private readonly MediaSessionService media;
    private readonly TaskbarDock dock;
    private GlobalHotkeys? hotkeys;

    private TrackInfo? currentTrack;

    public TaskbarWidget(MediaSessionService media)
    {
        InitializeComponent();

        this.media = media;
        dock = new TaskbarDock(this);
        dock.SuppressedChanged += UpdateVisibility;
        dock.SettingsChanged += ApplySettings;
        ApplySettings();

        media.TrackChanged += track => Dispatcher.InvokeAsync(() => ShowTrack(track));
    }

    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        dock.Start();
        RegisterHotkeys();
        await media.StartAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        hotkeys?.Dispose();
        base.OnClosed(e);
    }

    private void ApplySettings()
    {
        TrackTextPanel.Visibility = dock.Settings.ShowTrackText ? Visibility.Visible : Visibility.Collapsed;
        PlaceTrackText(dock.Settings.TextPlacement);
    }

    private void PlaceTrackText(TextPlacement placement)
    {
        var textOnLeft = placement == TextPlacement.Left;
        Layout.Children.Remove(TrackTextPanel);
        Layout.Children.Insert(textOnLeft ? 0 : Layout.Children.Count, TrackTextPanel);
        TrackTextPanel.Margin = textOnLeft ? new Thickness(4, 0, 6, 0) : new Thickness(6, 0, 4, 0);
        TrackTextPanel.HorizontalAlignment = textOnLeft ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    private void RegisterHotkeys()
    {
        hotkeys = new GlobalHotkeys(this);
        hotkeys.Register(GlobalHotkeys.ModControl, GlobalHotkeys.KeyRight, () => _ = media.NextAsync());
        hotkeys.Register(GlobalHotkeys.ModControl, GlobalHotkeys.KeyLeft, () => _ = media.PreviousAsync());
    }

    private void ShowTrack(TrackInfo? track)
    {
        var coverChanged = !ReferenceEquals(track?.Cover, currentTrack?.Cover);
        currentTrack = track;

        UpdateVisibility();
        if (track is null)
            return;

        TitleText.Text = track.Title;
        ArtistText.Text = track.Artist;
        PlayPauseIcon.Text = track.IsPlaying ? PauseGlyph : PlayGlyph;

        if (coverChanged)
            ShowCover(track.Cover);
    }

    private void ShowCover(byte[]? cover)
    {
        var image = cover is null ? null : CreateImage(cover);
        CoverImage.Background = image is null ? null : new ImageBrush(image) { Stretch = Stretch.UniformToFill };
        CoverPlaceholder.Visibility = image is null ? Visibility.Visible : Visibility.Collapsed;
    }

    private static BitmapImage? CreateImage(byte[] data)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 96;
            image.StreamSource = new MemoryStream(data);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void UpdateVisibility()
    {
        Root.Visibility = currentTrack is not null && !dock.IsSuppressed ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnPreviousClick(object sender, RoutedEventArgs e) => await media.PreviousAsync();

    private async void OnNextClick(object sender, RoutedEventArgs e) => await media.NextAsync();

    private async void OnCoverClick(object sender, MouseButtonEventArgs e) => await media.TogglePlayPauseAsync();
}
