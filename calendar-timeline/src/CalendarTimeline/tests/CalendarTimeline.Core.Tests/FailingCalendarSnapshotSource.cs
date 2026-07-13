using CalendarTimeline.Core;
using CalendarTimeline.Worker;

namespace CalendarTimeline.Core.Tests;

internal sealed class FailingCalendarSnapshotSource : ICalendarSnapshotSource
{
    public Task<CalendarSnapshot> LoadSnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("COM unavailable");
    }
}
