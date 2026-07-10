using CalendarTimeline.CommandPalette;
using CalendarTimeline.Core;

namespace CalendarTimeline.Core.Tests;

public sealed class FailingWorkerSnapshotClient : IWorkerSnapshotClient
{
    public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("worker failed");
    }
}
