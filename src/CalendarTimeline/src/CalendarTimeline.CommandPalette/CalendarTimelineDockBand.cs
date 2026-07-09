using CalendarTimeline.Core;

namespace CalendarTimeline.CommandPalette;

public sealed class CalendarTimelineDockBand
{
    private CalendarSnapshot? snapshot;

    public CalendarSnapshot? Snapshot => snapshot;

    public IReadOnlyList<TimelineBlock> Blocks { get; private set; } = [];

    public string StatusMessage { get; private set; } = string.Empty;

    public void Update(CalendarSnapshot nextSnapshot)
    {
        snapshot = nextSnapshot;
        Blocks = TimelineLayout.Arrange(nextSnapshot.Appointments);
        StatusMessage = nextSnapshot.StatusMessage ?? string.Empty;
    }

    public void ApplyWorkerError(string statusMessage)
    {
        StatusMessage = statusMessage;
    }
}
