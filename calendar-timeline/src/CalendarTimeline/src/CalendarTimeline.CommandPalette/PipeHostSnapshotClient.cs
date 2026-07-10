using CalendarTimeline.Core;
using CalendarTimeline.Ipc;

namespace CalendarTimeline.CommandPalette;

public sealed class PipeHostSnapshotClient : IHostSnapshotClient
{
    private readonly CalendarTimelinePipeClient pipeClient;

    public PipeHostSnapshotClient()
        : this(new CalendarTimelinePipeClient())
    {
    }

    public PipeHostSnapshotClient(CalendarTimelinePipeClient pipeClient)
    {
        this.pipeClient = pipeClient;
    }

    public async Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        return await pipeClient.SendAsync(new GetSnapshotRequest(), cancellationToken) switch
        {
            SnapshotResponse response => response.Snapshot,
            ErrorResponse error => throw new InvalidOperationException(error.Message),
            StatusResponse status => throw new InvalidOperationException(status.Status),
            _ => throw new InvalidOperationException("Unexpected host snapshot response."),
        };
    }
}
