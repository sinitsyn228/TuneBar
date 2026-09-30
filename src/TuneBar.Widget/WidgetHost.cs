using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using TuneBar.Media;
using TuneBar.Platform;
using TuneBar.Shared;
using TuneBar.Taskbar;

namespace TuneBar;

public sealed class WidgetHost : IDisposable
{
    private readonly MediaSessionService media;
    private readonly DispatcherTimer timer;
    private readonly GlobalHotkeys hotkeys = new();
    private readonly Dictionary<IntPtr, TaskbarWidget> widgets = new();
    private WidgetSettings settings = WidgetSettings.Load();
    private DateTime settingsSavedUtc = WidgetSettings.LastSavedUtc;
    private TrackInfo? currentTrack;

    public WidgetHost(MediaSessionService media)
    {
        this.media = media;
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => Refresh();

        media.TrackChanged += track => timer.Dispatcher.InvokeAsync(() => ShowTrack(track));
    }

    public async Task StartAsync()
    {
        RegisterHotkeys();
        Refresh();
        timer.Start();
        await media.StartAsync();
    }

    public void Dispose()
    {
        timer.Stop();
        hotkeys.Dispose();
    }

    private void RegisterHotkeys()
    {
        hotkeys.Register(GlobalHotkeys.ModControl, GlobalHotkeys.KeyRight, () => _ = media.NextAsync());
        hotkeys.Register(GlobalHotkeys.ModControl, GlobalHotkeys.KeyLeft, () => _ = media.PreviousAsync());
        hotkeys.Register(GlobalHotkeys.ModControl, GlobalHotkeys.KeySpace, () => _ = media.TogglePlayPauseAsync());
        hotkeys.Register(GlobalHotkeys.ModControl, GlobalHotkeys.KeyUp, () => VolumeControl.Raise(currentTrack?.SourceAppId), repeatWhileHeld: true);
        hotkeys.Register(GlobalHotkeys.ModControl, GlobalHotkeys.KeyDown, () => VolumeControl.Lower(currentTrack?.SourceAppId), repeatWhileHeld: true);
    }

    private void Refresh()
    {
        ReloadSettingsIfChanged();

        var taskbars = TaskbarDock.FindTaskbars(settings.Monitor);
        CloseWidgetsExcept(taskbars);

        foreach (var taskbar in taskbars)
        {
            if (!widgets.ContainsKey(taskbar))
                OpenWidget(taskbar);

            widgets[taskbar].Reposition();
        }
    }

    private void ReloadSettingsIfChanged()
    {
        var savedUtc = WidgetSettings.LastSavedUtc;
        if (savedUtc == settingsSavedUtc)
            return;

        settingsSavedUtc = savedUtc;
        settings = WidgetSettings.Load();

        foreach (var widget in widgets.Values)
            widget.ApplySettings(settings);
    }

    private void OpenWidget(IntPtr taskbar)
    {
        var widget = new TaskbarWidget(media, taskbar, settings);
        widget.ShowTrack(currentTrack);
        widget.Show();
        widgets[taskbar] = widget;
    }

    private void CloseWidgetsExcept(IReadOnlyList<IntPtr> taskbars)
    {
        foreach (var taskbar in widgets.Keys.Except(taskbars).ToList())
        {
            widgets[taskbar].Close();
            widgets.Remove(taskbar);
        }
    }

    private void ShowTrack(TrackInfo? track)
    {
        currentTrack = track;

        foreach (var widget in widgets.Values)
            widget.ShowTrack(track);
    }
}
