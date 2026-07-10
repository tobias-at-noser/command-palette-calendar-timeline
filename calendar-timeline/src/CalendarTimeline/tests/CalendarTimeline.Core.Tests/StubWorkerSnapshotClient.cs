using CalendarTimeline.CommandPalette;
using CalendarTimeline.Core;

namespace CalendarTimeline.Core.Tests;

public sealed class StubWorkerSnapshotClient(CalendarSnapshot snapshot) : IWorkerSnapshotClient
{
    public int Calls { get; private set; }

    public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(snapshot);
    }
}
