using CalendarTimeline.Core;

namespace CalendarTimeline.Host;

public interface IHostSnapshotSource
{
    Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken);
}
