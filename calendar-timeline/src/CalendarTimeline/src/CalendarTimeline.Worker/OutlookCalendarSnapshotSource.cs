using CalendarTimeline.Core;

namespace CalendarTimeline.Worker;

public sealed class OutlookCalendarSnapshotSource : ICalendarSnapshotSource
{
    public Task<CalendarSnapshot> LoadSnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
#if WINDOWS
        throw new NotImplementedException("Outlook COM calendar loading is not implemented yet.");
#else
        throw new PlatformNotSupportedException("Outlook COM calendar loading requires Windows and Outlook Desktop.");
#endif
    }
}
