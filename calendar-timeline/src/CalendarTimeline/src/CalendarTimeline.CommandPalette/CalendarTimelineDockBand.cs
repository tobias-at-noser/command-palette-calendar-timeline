using CalendarTimeline.Core;
#if WINDOWS
using Microsoft.CommandPalette.Extensions.Toolkit;
#endif

namespace CalendarTimeline.CommandPalette;

public sealed partial class CalendarTimelineDockBand
#if WINDOWS
    : WrappedDockItem
#endif
{
    private const string UnavailableMessage = "Kalenderdaten nicht verfügbar";
    private const int MaxVisibleRows = 3;
    private readonly IHostSnapshotClient hostSnapshotClient;
    private CalendarSnapshot? snapshot;

    public CalendarTimelineDockBand()
#if WINDOWS
        : base([], "calendar-timeline.dock-band", "Calendar Timeline")
#endif
    {
        hostSnapshotClient = new PipeHostSnapshotClient();
#if WINDOWS
        Icon = new IconInfo("\uE787");
        Title = "Calendar Timeline";
#endif
        Subtitle = "Warte auf Outlook-Kalenderdaten";
    }

    public CalendarTimelineDockBand(IHostSnapshotClient hostSnapshotClient)
#if WINDOWS
        : base([], "calendar-timeline.dock-band", "Calendar Timeline")
#endif
    {
        this.hostSnapshotClient = hostSnapshotClient;
#if WINDOWS
        Icon = new IconInfo("\uE787");
        Title = "Calendar Timeline";
#endif
        Subtitle = "Warte auf Outlook-Kalenderdaten";
    }

    public CalendarSnapshot? Snapshot => snapshot;

    public IReadOnlyList<DockAgendaItem> Rows { get; private set; } = [];

    public int VisibleItemCount { get; private set; }

    public string StatusMessage { get; private set; } = string.Empty;

    private string subtitle = string.Empty;

#if WINDOWS
    public override string Subtitle
#else
    public string Subtitle
#endif
    {
        get => subtitle;
        set => subtitle = value;
    }

    public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            Update(await hostSnapshotClient.LoadSnapshotAsync(cancellationToken));
            return true;
        }
        catch
        {
            ApplyHostError(UnavailableMessage);
            return false;
        }
    }

    public void Update(CalendarSnapshot nextSnapshot)
    {
        snapshot = nextSnapshot;
        Rows = DockAgendaProjector.Project(nextSnapshot, MaxVisibleRows);
        VisibleItemCount = Rows.Count;
        StatusMessage = nextSnapshot.StatusMessage ?? string.Empty;
        UpdateVisibleItems();
        Subtitle = BuildSubtitle();
    }

    public void ApplyHostError(string statusMessage)
    {
        StatusMessage = statusMessage;
        Rows = [new DockAgendaItem(statusMessage, string.Empty, 0, false, null, null)];
        VisibleItemCount = Rows.Count;
        UpdateVisibleItems();
        Subtitle = statusMessage;
    }

    private void UpdateVisibleItems()
    {
#if WINDOWS
        Items = Rows.Select(row => new ListItem(CreateRowCommand(row))
        {
            Title = row.Title,
            Subtitle = row.Subtitle,
            Icon = new IconInfo(row.IsRunning ? "\uE823" : "\uE787"),
        }).ToArray();
#endif
    }

#if WINDOWS
    private static Command CreateRowCommand(DockAgendaItem row)
    {
        if (row.TeamsUrl is not null)
        {
            return new OpenUrlCommand(row.TeamsUrl)
            {
                Name = "Teams öffnen",
            };
        }

        return new NoOpCommand();
    }
#endif

    private string BuildSubtitle()
    {
        if (!string.IsNullOrWhiteSpace(StatusMessage))
        {
            return StatusMessage;
        }

        if (snapshot is null || Rows.Count == 0)
        {
            return "Keine Termine im Zeitfenster";
        }

        return $"{Rows.Count} Termine · aktualisiert {snapshot.GeneratedAt:HH:mm}";
    }
}
