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
    private CalendarSnapshot? snapshot;

    public CalendarTimelineDockBand()
    {
#if WINDOWS
        Icon = new IconInfo("\uE787");
        Title = "Calendar Timeline";
#endif
        Subtitle = "Warte auf Outlook-Kalenderdaten";
    }

    public CalendarSnapshot? Snapshot => snapshot;

    public IReadOnlyList<TimelineBlock> Blocks { get; private set; } = [];

    public IReadOnlyList<TimelineRow> Rows { get; private set; } = [];

    public int VisibleItemCount { get; private set; }

    public string FreeTimeSummary { get; private set; } = string.Empty;

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

    public void Update(CalendarSnapshot nextSnapshot)
    {
        snapshot = nextSnapshot;
        Blocks = TimelineLayout.Arrange(nextSnapshot.Appointments);
        Rows = Blocks.Select(block => BuildRow(nextSnapshot.GeneratedAt, block)).ToArray();
        VisibleItemCount = Rows.Count;
        UpdateVisibleItems();
        FreeTimeSummary = BuildFreeTimeSummary(nextSnapshot);
        StatusMessage = nextSnapshot.StatusMessage ?? string.Empty;
        Subtitle = BuildSubtitle();
    }

    public void ApplyWorkerError(string statusMessage)
    {
        StatusMessage = statusMessage;
        Subtitle = statusMessage;
    }

    private static TimelineRow BuildRow(DateTimeOffset now, TimelineBlock block)
    {
        var appointment = block.Appointment;
        var isRunning = appointment.Start <= now && appointment.End > now;
        var title = isRunning ? $"Jetzt · {appointment.Title}" : appointment.Title;
        var subtitleParts = new List<string>
        {
            isRunning ? $"{FormatDuration(appointment.End - now)} verbleibend" : $"{appointment.Start:HH:mm}–{appointment.End:HH:mm}",
        };

        if (!string.IsNullOrWhiteSpace(appointment.Location))
        {
            subtitleParts.Add(appointment.Location);
        }

        return new TimelineRow(title, string.Join(" · ", subtitleParts), block.Lane, isRunning, appointment.TeamsUrl);
    }

    private static string BuildFreeTimeSummary(CalendarSnapshot snapshot)
    {
        var running = snapshot.Appointments.Any(appointment => appointment.Start <= snapshot.GeneratedAt && appointment.End > snapshot.GeneratedAt);

        if (running)
        {
            return string.Empty;
        }

        var next = snapshot.Appointments
            .Where(appointment => appointment.Start > snapshot.GeneratedAt)
            .OrderBy(appointment => appointment.Start)
            .FirstOrDefault();

        return next is null ? "Keine weiteren Termine" : $"Nächster Termin in {FormatDuration(next.Start - snapshot.GeneratedAt)}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var minutes = Math.Max(0, (int)Math.Ceiling(duration.TotalMinutes));

        if (minutes < 60)
        {
            return $"{minutes} Min.";
        }

        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;
        return remainingMinutes == 0 ? $"{hours} Std." : $"{hours} Std. {remainingMinutes} Min.";
    }

    private void UpdateVisibleItems()
    {
#if WINDOWS
        Items = Rows.Select(row => new ListItem(CreateRowCommand(row))
        {
            Title = row.Title,
            Subtitle = $"Spur {row.Lane + 1} · {row.Subtitle}",
            Icon = new IconInfo(row.IsRunning ? "\uE823" : "\uE787"),
        }).ToArray();
#endif
    }

#if WINDOWS
    private static Command CreateRowCommand(TimelineRow row)
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

        if (snapshot is null || snapshot.Appointments.Count == 0)
        {
            return "Keine Termine im Zeitfenster";
        }

        return $"{snapshot.Appointments.Count} Termine · aktualisiert {snapshot.GeneratedAt:HH:mm}";
    }
}
