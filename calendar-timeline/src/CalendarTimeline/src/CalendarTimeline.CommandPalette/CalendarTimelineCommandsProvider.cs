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
    private readonly CalendarTimelineDockBand dockBand;
#if WINDOWS
    private readonly ICommandItem[] commands;
    private readonly ICommandItem[] dockBands;
#endif

    public CalendarTimelineCommandsProvider()
        : this(new PipeHostSnapshotClient())
    {
    }

    public CalendarTimelineCommandsProvider(IHostSnapshotClient hostSnapshotClient)
    {
        HostSnapshotClient = hostSnapshotClient;
        dockBand = new CalendarTimelineDockBand(hostSnapshotClient);
#if WINDOWS
        DisplayName = "Calendar Timeline";
        Id = "calendar-timeline.command-palette";
        Icon = new IconInfo("\uE787");
        commands = [new CommandItem(new NoOpCommand()) { Title = DisplayName, Subtitle = "Outlook calendar timeline dock band", Icon = Icon }];
        dockBands = [dockBand];
#endif
    }

    public CalendarTimelineDockBand DockBand => dockBand;

    public IHostSnapshotClient HostSnapshotClient { get; }

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

    public Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        return dockBand.RefreshAsync(cancellationToken);
    }
}
