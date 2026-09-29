using TuneBar.Shared;

namespace TuneBar.Launcher;

internal sealed class PositionMenu
{
    private const int MaxOffsetPixels = 2000;

    private readonly WidgetSettings settings;

    public PositionMenu(WidgetSettings settings)
    {
        this.settings = settings;
    }

    public static string Describe(WidgetSettings settings) =>
        $"{PositionName(settings.Position)}, отступ {settings.OffsetPixels} px";

    public void Run()
    {
        new ConsoleMenu(
            "TuneBar · Положение",
            [
                Choice('1', WidgetPosition.Left),
                Choice('2', WidgetPosition.BeforeIcons),
                Choice('3', WidgetPosition.Right),
                new MenuItem('4', "Отступ от края", () => $"{settings.OffsetPixels} px", EditOffset),
                MenuItem.Back('0', "Назад"),
            ]).Run();
    }

    private MenuItem Choice(char key, WidgetPosition position) =>
        new(key, PositionName(position), () => settings.Position == position ? "●" : string.Empty, () =>
        {
            settings.Position = position;
            return Save();
        });

    private string? EditOffset()
    {
        var input = ConsoleMenu.ReadLine($"Новый отступ в пикселях (0–{MaxOffsetPixels}): ");
        if (!int.TryParse(input, out var offset) || offset is < 0 or > MaxOffsetPixels)
            return "Нужно целое число.";

        settings.OffsetPixels = offset;
        return Save();
    }

    private string Save()
    {
        settings.Save();
        return WidgetInstance.IsRunning
            ? "Сохранено, виджет переместится через секунду."
            : "Сохранено, применится при включении виджета.";
    }

    private static string PositionName(WidgetPosition position) => position switch
    {
        WidgetPosition.BeforeIcons => "перед значками",
        WidgetPosition.Right => "справа, у трея",
        _ => "слева",
    };
}
