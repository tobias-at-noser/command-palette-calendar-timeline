namespace CalendarTimeline.Host;

#if WINDOWS
using System.Drawing;
using System.Windows.Forms;
using CalendarTimeline.Ipc;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly CancellationToken cancellationToken;
    private readonly Action requestShutdown;
    private readonly CalendarTimelineHostService service;
    private readonly HostSettingsStore settingsStore;
    private readonly SnapbarProcessController snapbarProcessController;
    private readonly AutostartManager autostartManager;
    private readonly Control shutdownDispatcher;
    private readonly NotifyIcon notifyIcon;
    private ToolStripMenuItem? autostartMenuItem;
    private HostSettings settings = HostSettings.Default;

    public TrayApplicationContext(CalendarTimelineHostService service, CancellationToken cancellationToken, Action requestShutdown)
        : this(service, cancellationToken, requestShutdown, new HostSettingsStore(), new SnapbarProcessController(), new AutostartManager())
    {
    }

    internal TrayApplicationContext(
        CalendarTimelineHostService service,
        CancellationToken cancellationToken,
        Action requestShutdown,
        HostSettingsStore settingsStore,
        SnapbarProcessController snapbarProcessController,
        AutostartManager autostartManager)
    {
        this.service = service;
        this.cancellationToken = cancellationToken;
        this.requestShutdown = requestShutdown;
        this.settingsStore = settingsStore;
        this.snapbarProcessController = snapbarProcessController;
        this.autostartManager = autostartManager;
        shutdownDispatcher = new Control();
        shutdownDispatcher.CreateControl();
        settings = settingsStore.Load();
        notifyIcon = new NotifyIcon
        {
            Text = "Calendar Timeline Host",
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = CreateMenu()
        };
        UpdateAutostartMenuText();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            shutdownDispatcher.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Timeline anzeigen", null, async (_, _) => await ShowTimelineAsync());
        menu.Items.Add("Timeline verbergen", null, async (_, _) => await HideTimelineAsync());
        autostartMenuItem = new ToolStripMenuItem();
        autostartMenuItem.Click += async (_, _) => await ToggleAutostartAsync();
        menu.Items.Add(autostartMenuItem);
        menu.Items.Add("Jetzt aktualisieren", null, async (_, _) =>
        {
            try
            {
                await service.HandleAsync(new RefreshSnapshotRequest(), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        });
        menu.Items.Add("Beenden", null, (_, _) =>
        {
            requestShutdown();
            ExitThread();
        });
        return menu;
    }

    internal void ExitThreadSafely()
    {
        if (shutdownDispatcher.IsDisposed)
        {
            return;
        }

        try
        {
            if (shutdownDispatcher.InvokeRequired)
            {
                shutdownDispatcher.BeginInvoke((Action)ExitThread);
                return;
            }

            ExitThread();
        }
        catch (InvalidOperationException) when (shutdownDispatcher.IsDisposed || shutdownDispatcher.Disposing)
        {
        }
    }

    private async Task ShowTimelineAsync()
    {
        var status = await snapbarProcessController.ShowAsync(cancellationToken);
        settings = settings with { ShowSnapbar = true };
        await settingsStore.SaveAsync(settings, cancellationToken);
        notifyIcon.Text = TrimText(status);
    }

    private async Task HideTimelineAsync()
    {
        var status = await snapbarProcessController.HideAsync(cancellationToken);
        settings = settings with { ShowSnapbar = false };
        await settingsStore.SaveAsync(settings, cancellationToken);
        notifyIcon.Text = TrimText(status);
    }

    private async Task ToggleAutostartAsync()
    {
        var enabled = !autostartManager.IsEnabled();
        autostartManager.SetEnabled(enabled);
        settings = settings with { AutostartEnabled = enabled };
        await settingsStore.SaveAsync(settings, cancellationToken);
        UpdateAutostartMenuText();
    }

    private void UpdateAutostartMenuText()
    {
        if (autostartMenuItem is not null)
        {
            autostartMenuItem.Text = autostartManager.IsEnabled() ? "Autostart deaktivieren" : "Autostart aktivieren";
        }
    }

    private static string TrimText(string text)
    {
        const int maxLength = 63;
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
#else
public sealed class TrayApplicationContext : IDisposable
{
    public TrayApplicationContext(CalendarTimelineHostService service, CancellationToken cancellationToken, Action requestShutdown)
    {
    }

    public void Dispose()
    {
    }
}
#endif
