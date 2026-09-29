using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TuneBar.Shared;

public enum WidgetPosition
{
    Left,
    BeforeIcons,
    Right,
}

public enum TextPlacement
{
    Right,
    Left,
}

public sealed class WidgetSettings
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TuneBar");

    private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public WidgetPosition Position { get; set; } = WidgetPosition.Left;

    public int OffsetPixels { get; set; } = 12;

    public bool ShowTrackText { get; set; } = true;

    public TextPlacement TextPlacement { get; set; } = TextPlacement.Right;

    public static WidgetSettings Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<WidgetSettings>(File.ReadAllText(FilePath), JsonOptions) ?? new WidgetSettings()
                : new WidgetSettings();
        }
        catch (Exception)
        {
            return new WidgetSettings();
        }
    }

    public static DateTime LastSavedUtc =>
        File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : DateTime.MinValue;

    public void Save()
    {
        Directory.CreateDirectory(FolderPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
