using CalendarTimeline.Core;

namespace CalendarTimeline.CommandPalette;

public interface IHostSnapshotClient
{
    Task<CalendarSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken);
}
