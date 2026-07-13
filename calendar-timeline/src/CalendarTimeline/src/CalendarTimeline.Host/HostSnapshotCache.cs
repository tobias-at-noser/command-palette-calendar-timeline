using CalendarTimeline.Core;
using CalendarTimeline.Ipc;

namespace CalendarTimeline.Host;

public sealed class HostSnapshotCache
{
    public const string UnavailableStatus = "Kalenderdaten nicht verfügbar";

    private CalendarSnapshot? snapshot;

    public string Status { get; private set; } = UnavailableStatus;

    public CalendarSnapshot? Snapshot => snapshot;

    public void Update(CalendarSnapshot snapshot, string status)
    {
        this.snapshot = snapshot;
        Status = status;
    }

    public void MarkUnavailable(string status = UnavailableStatus)
    {
        snapshot = null;
        Status = status;
    }

    public CalendarTimelineResponse GetSnapshotResponse()
    {
        return snapshot is null
            ? new ErrorResponse(Status)
            : new SnapshotResponse(snapshot);
    }
}
