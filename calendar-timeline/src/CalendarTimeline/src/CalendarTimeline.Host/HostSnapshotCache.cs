using CalendarTimeline.Core;
using CalendarTimeline.Ipc;

namespace CalendarTimeline.Host;

public sealed class HostSnapshotCache
{
    private CalendarSnapshot? snapshot;

    public string Status { get; private set; } = "Kalenderdaten nicht verfügbar";

    public CalendarSnapshot? Snapshot => snapshot;

    public void Update(CalendarSnapshot snapshot, string status)
    {
        this.snapshot = snapshot;
        Status = status;
    }

    public void MarkUnavailable()
    {
        snapshot = null;
        Status = "Kalenderdaten nicht verfügbar";
    }

    public CalendarTimelineResponse GetSnapshotResponse()
    {
        return snapshot is null
            ? new ErrorResponse("Kalenderdaten nicht verfügbar")
            : new SnapshotResponse(snapshot);
    }
}
