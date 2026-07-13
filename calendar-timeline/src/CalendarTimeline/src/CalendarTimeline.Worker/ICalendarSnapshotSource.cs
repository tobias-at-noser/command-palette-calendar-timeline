using CalendarTimeline.Core;

namespace CalendarTimeline.Worker;

public interface ICalendarSnapshotSource
{
    Task<CalendarSnapshot> LoadSnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
