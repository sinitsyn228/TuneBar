using System;
using System.Text;

namespace TuneBar.Launcher;

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.Title = "TuneBar";

        new MainMenu().Run();
    }
}
