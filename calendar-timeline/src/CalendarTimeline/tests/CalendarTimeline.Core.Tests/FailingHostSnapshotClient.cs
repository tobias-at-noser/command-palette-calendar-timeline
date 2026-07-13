using CalendarTimeline.CommandPalette;
using CalendarTimeline.Core;

namespace CalendarTimeline.Core.Tests;

public sealed class FailingHostSnapshotClient : IHostSnapshotClient
{
    public Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("host failed");
    }
}
