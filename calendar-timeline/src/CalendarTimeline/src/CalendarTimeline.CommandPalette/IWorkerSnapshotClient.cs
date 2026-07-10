using CalendarTimeline.Core;

namespace CalendarTimeline.CommandPalette;

public interface IWorkerSnapshotClient
{
    Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken);
}
