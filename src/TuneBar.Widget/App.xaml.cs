using System.Threading;
using System.Windows;
using System.Windows.Media;
using TuneBar.Media;
using TuneBar.Platform;
using TuneBar.Shared;

namespace TuneBar;

public partial class App : Application
{
    private Mutex? singleInstanceMutex;
    private EventWaitHandle? exitSignal;
    private RegisteredWaitHandle? exitSignalRegistration;
    private TrayIcon? trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        singleInstanceMutex = new Mutex(true, WidgetInstance.MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            Shutdown();
            return;
        }

        ListenForExitSignal();
        ApplyTheme();

        var widget = new TaskbarWidget(new MediaSessionService());
        widget.Show();

        trayIcon = new TrayIcon(Shutdown);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        exitSignalRegistration?.Unregister(null);
        exitSignal?.Dispose();
        trayIcon?.Dispose();
        singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void ListenForExitSignal()
    {
        exitSignal = new EventWaitHandle(false, EventResetMode.AutoReset, WidgetInstance.ExitEventName);
        exitSignalRegistration = ThreadPool.RegisterWaitForSingleObject(
            exitSignal,
            (_, _) => Dispatcher.InvokeAsync(Shutdown),
            null,
            Timeout.Infinite,
            executeOnlyOnce: true);
    }

    private void ApplyTheme()
    {
        if (!SystemTheme.IsTaskbarLight())
            return;

        Resources["WidgetForeground"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
        Resources["WidgetSecondaryForeground"] = new SolidColorBrush(Color.FromArgb(0xB3, 0x1A, 0x1A, 0x1A));
        Resources["WidgetHover"] = new SolidColorBrush(Color.FromArgb(0x1F, 0x00, 0x00, 0x00));
    }
}
