using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TuneBar.Shared;

namespace TuneBar.Platform;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon notifyIcon;

    public TrayIcon(Action onExit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Настройки", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => onExit());

        notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "TuneBar",
            ContextMenuStrip = menu,
            Visible = true,
        };
        notifyIcon.DoubleClick += (_, _) => OpenSettings();
    }

    public void Dispose()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
    }

    private static void OpenSettings()
    {
        if (File.Exists(WidgetInstance.LauncherPath))
            Process.Start(new ProcessStartInfo(WidgetInstance.LauncherPath) { UseShellExecute = true });
    }
}
