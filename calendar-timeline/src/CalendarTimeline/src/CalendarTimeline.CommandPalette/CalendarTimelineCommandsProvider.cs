using CalendarTimeline.Core;
#if WINDOWS
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
#endif

namespace CalendarTimeline.CommandPalette;

public sealed partial class CalendarTimelineCommandsProvider
#if WINDOWS
    : CommandProvider
#endif
{
    private readonly CalendarTimelineDockBand dockBand = new();
    private readonly IWorkerSnapshotClient workerClient;
#if WINDOWS
    private readonly ICommandItem[] commands;
    private readonly ICommandItem[] dockBands;
#endif

    public CalendarTimelineCommandsProvider()
        : this(new WorkerProcessSnapshotClient())
    {
    }

    public CalendarTimelineCommandsProvider(IWorkerSnapshotClient workerClient)
    {
        this.workerClient = workerClient;
#if WINDOWS
        DisplayName = "Calendar Timeline";
        Id = "calendar-timeline.command-palette";
        Icon = new IconInfo("\uE787");
        commands = [new CommandItem(new NoOpCommand()) { Title = DisplayName, Subtitle = "Outlook calendar timeline dock band", Icon = Icon }];
        dockBands = [new WrappedDockItem([dockBand], "calendar-timeline.dock-band", "Calendar Timeline") { Icon = Icon }];
#endif
    }

    public CalendarTimelineDockBand DockBand => dockBand;

#if WINDOWS
    public override ICommandItem[] TopLevelCommands()
    {
        return commands;
    }

    public override ICommandItem[] GetDockBands()
    {
        return dockBands;
    }
#endif

    public void Update(CalendarSnapshot snapshot)
    {
        dockBand.Update(snapshot);
    }

    public async Task<bool> RefreshFromWorkerAsync(CancellationToken cancellationToken)
    {
        try
        {
            dockBand.Update(await workerClient.LoadSnapshotAsync(cancellationToken));
            return true;
        }
        catch
        {
            dockBand.ApplyWorkerError("Outlook-Kalender nicht verfügbar");
            return false;
        }
    }

    public bool TryApplyWorkerSnapshot(string json)
    {
        try
        {
            dockBand.Update(CalendarSnapshotJson.Deserialize(json));
            return true;
        }
        catch
        {
            dockBand.ApplyWorkerError("Outlook-Kalender nicht verfügbar");
            return false;
        }
    }
}
