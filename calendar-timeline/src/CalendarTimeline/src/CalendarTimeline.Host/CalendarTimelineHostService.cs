using CalendarTimeline.Ipc;

namespace CalendarTimeline.Host;

public sealed class CalendarTimelineHostService
{
    private readonly HostSnapshotCache cache;
    private readonly IHostSnapshotSource snapshotSource;
    private readonly SemaphoreSlim refreshGate = new(1, 1);

    public CalendarTimelineHostService(HostSnapshotCache cache, IHostSnapshotSource snapshotSource)
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
        await refreshGate.WaitAsync(cancellationToken);
        try
        {
            var snapshot = await snapshotSource.LoadSnapshotAsync(cancellationToken);
            cache.Update(snapshot, "ok");
            return cache.GetSnapshotResponse();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            cache.MarkUnavailable();
            return cache.GetSnapshotResponse();
        }
        finally
        {
            refreshGate.Release();
        }
    }
}
