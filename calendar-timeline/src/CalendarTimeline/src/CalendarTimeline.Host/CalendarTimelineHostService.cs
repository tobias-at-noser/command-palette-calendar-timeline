using CalendarTimeline.Ipc;

namespace CalendarTimeline.Host;

public sealed class CalendarTimelineHostService
{
    private readonly HostSnapshotCache cache;
    private readonly FakeHostSnapshotSource snapshotSource;

    public CalendarTimelineHostService(HostSnapshotCache cache, FakeHostSnapshotSource snapshotSource)
    {
        this.cache = cache;
        this.snapshotSource = snapshotSource;
    }

    public async Task<CalendarTimelineResponse> HandleAsync(CalendarTimelineRequest request, CancellationToken cancellationToken)
    {
        return request switch
        {
            PingRequest => new StatusResponse("ok"),
            GetSnapshotRequest => cache.GetSnapshotResponse(),
            RefreshSnapshotRequest => await RefreshAsync(cancellationToken),
            _ => new ErrorResponse("Unbekannte Anfrage")
        };
    }

    private async Task<CalendarTimelineResponse> RefreshAsync(CancellationToken cancellationToken)
    {
        var snapshot = await snapshotSource.LoadSnapshotAsync(DateTimeOffset.Now, cancellationToken);
        cache.Update(snapshot, "ok");
        return cache.GetSnapshotResponse();
    }
}
