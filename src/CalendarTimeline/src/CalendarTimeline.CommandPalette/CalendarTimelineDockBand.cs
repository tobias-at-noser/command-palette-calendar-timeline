using CalendarTimeline.Core;

namespace CalendarTimeline.CommandPalette;

public sealed class CalendarTimelineDockBand
{
    private CalendarSnapshot? snapshot;

    public CalendarSnapshot? Snapshot => snapshot;

    public IReadOnlyList<TimelineBlock> Blocks { get; private set; } = [];

    public void Update(CalendarSnapshot nextSnapshot)
    {
        snapshot = nextSnapshot;
        Blocks = TimelineLayout.Arrange(nextSnapshot.Appointments);
    }
}
