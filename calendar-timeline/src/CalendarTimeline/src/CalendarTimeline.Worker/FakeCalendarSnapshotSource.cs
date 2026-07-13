using CalendarTimeline.Core;

namespace CalendarTimeline.Worker;

public sealed class FakeCalendarSnapshotSource : ICalendarSnapshotSource
{
    public Task<CalendarSnapshot> LoadSnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        return Task.FromResult(FakeSnapshotFactory.Create(now));
    }
}
