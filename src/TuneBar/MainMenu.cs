using TuneBar.Shared;

namespace TuneBar.Launcher;

internal sealed class MainMenu
{
    private const string WidgetMissing = "Не найден TuneBar.Widget.exe рядом с TuneBar.exe.";

    private readonly WidgetSettings settings = WidgetSettings.Load();

    public void Run()
    {
        new ConsoleMenu(
            "TuneBar",
            [
                new MenuItem('1', "Включить", () => OnOff(WidgetInstance.IsRunning), ToggleWidget),
                new MenuItem('2', "Автозагрузка", () => OnOff(Autostart.IsEnabled), ToggleAutostart),
                new MenuItem('3', "Положение", () => PositionMenu.Describe(settings), OpenPositionMenu),
                new MenuItem('4', "Трек и артист", () => OnOff(settings.ShowTrackText), ToggleTrackText),
                new MenuItem('5', "Место текста", () => TextPlacementName(settings.TextPlacement), ToggleTextPlacement),
                new MenuItem('6', "Монитор", () => MonitorName(settings.Monitor), SwitchMonitor),
                MenuItem.Back('0', "Выход"),
            ],
            WidgetInstance.IsInstalled ? null : WidgetMissing).Run();
    }

    private static string OnOff(bool enabled) => enabled ? "вкл" : "выкл";

    private static string TextPlacementName(TextPlacement placement) =>
        placement == TextPlacement.Left ? "слева от кнопок" : "справа от кнопок";

    private static string MonitorName(WidgetMonitor monitor) => monitor switch
    {
        WidgetMonitor.Secondary => "дополнительный",
        WidgetMonitor.All => "все",
        _ => "основной",
    };

    private string? OpenPositionMenu()
    {
        new PositionMenu(settings).Run();
        return null;
    }

    private string? ToggleTrackText()
    {
        settings.ShowTrackText = !settings.ShowTrackText;
        settings.Save();
        return null;
    }

    private string? ToggleTextPlacement()
    {
        settings.TextPlacement = settings.TextPlacement == TextPlacement.Left ? TextPlacement.Right : TextPlacement.Left;
        settings.Save();
        return null;
    }

    private string? SwitchMonitor()
    {
        settings.Monitor = settings.Monitor switch
        {
            WidgetMonitor.Primary => WidgetMonitor.Secondary,
            WidgetMonitor.Secondary => WidgetMonitor.All,
            _ => WidgetMonitor.Primary,
        };
        settings.Save();
        return null;
    }

    private static string? ToggleWidget()
    {
        if (WidgetInstance.IsRunning)
            return WidgetInstance.Stop() ? null : "Не удалось выключить виджет.";

        if (!WidgetInstance.IsInstalled)
            return WidgetMissing;

        return WidgetInstance.Start() ? null : "Виджет не запустился.";
    }

    private static string? ToggleAutostart()
    {
        if (!Autostart.IsEnabled && !WidgetInstance.IsInstalled)
            return WidgetMissing;

        Autostart.SetEnabled(!Autostart.IsEnabled, WidgetInstance.ExecutablePath);
        return null;
    }
}
