using CalendarTimeline.Core;
#if WINDOWS
using Microsoft.CommandPalette.Extensions.Toolkit;
#endif

namespace CalendarTimeline.CommandPalette;

public sealed partial class CalendarTimelineDockBand
#if WINDOWS
    : ListItem
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
        StatusMessage = nextSnapshot.StatusMessage ?? string.Empty;
        Subtitle = BuildSubtitle();
    }

    public void ApplyWorkerError(string statusMessage)
    {
        StatusMessage = statusMessage;
        Subtitle = statusMessage;
    }

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
