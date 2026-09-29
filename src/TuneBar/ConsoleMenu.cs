using System;
using System.Collections.Generic;

namespace TuneBar.Launcher;

internal sealed record MenuItem(char Key, string Label, Func<string>? Value, Func<string?> Activate, bool ClosesMenu = false)
{
    public static MenuItem Back(char key, string label) => new(key, label, null, () => null, ClosesMenu: true);
}

internal sealed class ConsoleMenu
{
    private const int LabelWidth = 16;

    private readonly string title;
    private readonly IReadOnlyList<MenuItem> items;
    private int selectedIndex;
    private string message;

    public ConsoleMenu(string title, IReadOnlyList<MenuItem> items, string? initialMessage = null)
    {
        this.title = title;
        this.items = items;
        message = initialMessage ?? string.Empty;
    }

    public static string? ReadLine(string prompt)
    {
        Console.CursorVisible = true;
        Console.Write($"  {prompt}");
        var input = Console.ReadLine();
        Console.CursorVisible = false;
        return input;
    }

    public void Run()
    {
        Console.CursorVisible = false;
        Console.Clear();

        while (true)
        {
            Render();
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    MoveSelection(-1);
                    continue;
                case ConsoleKey.DownArrow:
                    MoveSelection(1);
                    continue;
                case ConsoleKey.Escape:
                    return;
                case ConsoleKey.Enter:
                    if (Activate(items[selectedIndex]))
                        return;
                    continue;
            }

            var index = IndexOfKey(key.KeyChar);
            if (index < 0)
                continue;

            selectedIndex = index;
            if (Activate(items[index]))
                return;
        }
    }

    private void MoveSelection(int step)
    {
        selectedIndex = (selectedIndex + step + items.Count) % items.Count;
    }

    private int IndexOfKey(char key)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Key == key)
                return i;
        }

        return -1;
    }

    private bool Activate(MenuItem item)
    {
        if (item.ClosesMenu)
            return true;

        message = item.Activate() ?? string.Empty;
        Console.CursorVisible = false;
        Console.Clear();
        return false;
    }

    private void Render()
    {
        Console.SetCursorPosition(0, 0);
        WriteLine(string.Empty);
        WriteLine($"  {title}", ConsoleColor.Cyan);
        WriteLine(string.Empty);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.ClosesMenu)
                WriteLine(string.Empty);

            var selected = i == selectedIndex;
            var value = item.Value?.Invoke();
            var suffix = string.IsNullOrEmpty(value) ? string.Empty : $": {value}";
            var marker = selected ? "›" : " ";
            WriteLine(
                $"  {marker} {item.Key} - {item.Label.PadRight(LabelWidth)}{suffix}",
                selected ? ConsoleColor.Green : ConsoleColor.Gray);
        }

        WriteLine(string.Empty);
        WriteLine("  ↑↓ — выбор   Enter — подтвердить   Esc — назад", ConsoleColor.DarkGray);
        WriteLine(string.Empty);
        WriteLine(string.IsNullOrEmpty(message) ? string.Empty : $"  {message}", ConsoleColor.Yellow);
    }

    private static void WriteLine(string text, ConsoleColor color = ConsoleColor.Gray)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text.PadRight(Math.Max(Console.WindowWidth - 1, text.Length)));
        Console.ResetColor();
    }
}
